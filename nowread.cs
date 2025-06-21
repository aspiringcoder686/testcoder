public static string ConvertToNativeSql(string hql, string entityName, string tableName, EntityDefinition entity, Dictionary<string, EntityDefinition> entityMap =null)
{
    if (string.IsNullOrWhiteSpace(hql))
        return hql;

    hql = hql.Trim();

    // Detect alias
    Regex aliasPattern = new($"from\\s+{entityName}\\s+(\\w+)", RegexOptions.IgnoreCase);
    Match aliasMatch = aliasPattern.Match(hql);
    string alias = aliasMatch.Success && !string.IsNullOrWhiteSpace(aliasMatch.Groups[1].Value)
        ? aliasMatch.Groups[1].Value
        : entityName; // fallback

    string aliasSql = "";
    if (!IsSqlKeyword(alias))
    {
        aliasSql = $"[{alias}]";
    }
    else
    {
        aliasSql = $"{alias}";
    }
    string fromClause = $"FROM {tableName} {aliasSql}";

    if (hql.StartsWith("from", StringComparison.OrdinalIgnoreCase))
    {
        hql = Regex.Replace(hql, $"from\\s+{entityName}(\\s+\\w+)?", fromClause, RegexOptions.IgnoreCase);
        hql = "SELECT * " + hql;
    }
    else
    {
        hql = Regex.Replace(hql, $"select\\s+(distinct\\s+)?\\w+\\s+from\\s+{entityName}\\s+\\w+", $"SELECT $1* {fromClause}", RegexOptions.IgnoreCase);
    }

    hql = hql.Replace("fetch", "", StringComparison.OrdinalIgnoreCase);

    // Replace ? parameters
    hql = MyRegex2().Replace(hql, match =>
    {
        string property = match.Groups[2].Value;
        return match.Value.Replace("?", "@" + property);
    });

    hql = MyRegex3().Replace(hql, match =>
    {
        string property = match.Groups[2].Value;
        return match.Value.Replace("?", "@" + property);
    });

    hql = MyRegex4().Replace(hql, match =>
    {
        string leftSide = match.Groups[1].Value;
        return $"{leftSide} between @Min and @Max";
    });

    // Replace boolean literals
    hql = Regex.Replace(hql, @"\btrue\b", "1", RegexOptions.IgnoreCase);
    hql = Regex.Replace(hql, @"\bfalse\b", "0", RegexOptions.IgnoreCase);

    // Replace alias references, only if alias is valid
    if (!string.IsNullOrWhiteSpace(alias) && !IsSqlKeyword(alias))
    {
        Regex aliasRefPattern = new($"\\b{alias}\\.(\\w+)", RegexOptions.IgnoreCase);
        hql = aliasRefPattern.Replace(hql, m => $"[{alias}].{m.Groups[1].Value}");
    }

    // Replace property names with column names
    foreach (var prop in entity.Properties)
    {
        if (!string.IsNullOrWhiteSpace(prop.Name) && !string.IsNullOrWhiteSpace(prop.Column) &&
            !prop.Name.Equals(prop.Column, StringComparison.OrdinalIgnoreCase))
        {
            string propPattern = $"\\[{alias}\\]\\.{Regex.Escape(prop.Name)}\\b";
            hql = Regex.Replace(hql, propPattern, $"[{alias}].{prop.Column}", RegexOptions.IgnoreCase);
        }
    }

    // Replace relationship properties
    foreach (var rel in entity.Relationships)
    {
        if (!string.IsNullOrWhiteSpace(rel.Name) && !string.IsNullOrWhiteSpace(rel.SourceColumn))
        {
            string relPattern = $"\\[{alias}\\]\\.{Regex.Escape(rel.Name)}\\.Id";
            hql = Regex.Replace(hql, relPattern, $"[{alias}].{rel.SourceColumn}", RegexOptions.IgnoreCase);
            hql = Regex.Replace(hql, $"@{Regex.Escape(rel.Name)}\\.Id\\b", $"@{rel.SourceColumn}", RegexOptions.IgnoreCase);
        }
    }

    hql = handleRelationshipBasedQueries(hql, entity, entityMap, alias);

    hql = hql.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
    return hql;
}

private static string handleRelationshipBasedQueries(string hql, EntityDefinition entity, Dictionary<string, EntityDefinition> entityMap, string alias)
{
    // Replace direct relationship nav with SourceColumn
    foreach (var rel in entity.Relationships)
    {
        if (!string.IsNullOrWhiteSpace(rel.Name) && !string.IsNullOrWhiteSpace(rel.SourceColumn))
        {
            // Replace only when it's a standalone property, not part of navigation (no trailing dot)

            // Pattern: alias.RelName (but NOT alias.RelName.*)
            string patternWithAlias = $@"\b{alias}\.{Regex.Escape(rel.Name)}\b(?!\.)";
            hql = Regex.Replace(hql, patternWithAlias, $"[{alias}].{rel.SourceColumn}", RegexOptions.IgnoreCase);

            // Pattern: RelName (no alias, NOT part of navigation)
            string patternNoAlias = $@"\b{Regex.Escape(rel.Name)}\b(?!\.)";
            hql = Regex.Replace(hql, patternNoAlias, rel.SourceColumn, RegexOptions.IgnoreCase);
        }
    }


    // Replace navigation to .Id → use FK directly
    foreach (var rel in entity.Relationships)
    {
        if (!string.IsNullOrWhiteSpace(rel.Name) && !string.IsNullOrWhiteSpace(rel.SourceColumn))
        {
            // Pattern: alias.Relation.Id (replace with [alias].SourceColumn)
            string patternNavAlias = $@"\b{alias}\.{Regex.Escape(rel.Name)}\.Id\b";
            hql = Regex.Replace(hql, patternNavAlias, $"[{alias}].{rel.SourceColumn}", RegexOptions.IgnoreCase);

            // Pattern: Relation.Id (no alias case)
            string patternNavNoAlias = $@"\b{Regex.Escape(rel.Name)}\.Id\b";
            hql = Regex.Replace(hql, patternNavNoAlias, $"{rel.SourceColumn}", RegexOptions.IgnoreCase);
        }
    }

    var tableJoins = new List<string>();

    // Detect nested references like va.Period.EndDate

    var test = "from HierarchyViewContentAsset va where va.DeletedUser is null and va.HierarchyView.Id  = ? And va.Period.EndDate = ?";
    var matches1 = Regex.Matches(test, @"\b(\w+)\.(\w+)\.(\w+)\b", RegexOptions.IgnoreCase);

    foreach (Match m in matches1)
    {
        Console.WriteLine($"Alias: {m.Groups[1].Value}, Rel: {m.Groups[2].Value}, Prop: {m.Groups[3].Value}");
    }


    var navPropPattern = new Regex(@"(\[(?<alias>\w+)\]|\b(?<alias>\w+))\.(?<rel>\w+)\.(?<prop>\w+)", RegexOptions.IgnoreCase);
    var navMatches = navPropPattern.Matches(hql);

    var joinFragments = new List<string>();
    var joinConditions = new List<string>();

    foreach (Match m in navMatches)
    {
        string aliasPrefix = m.Groups["alias"].Value;
        string navName = m.Groups["rel"].Value;
        string propName = m.Groups["prop"].Value;

        var rel = entity.Relationships.FirstOrDefault(r => r.Name.Equals(navName, StringComparison.OrdinalIgnoreCase));
        if (rel != null && entityMap.TryGetValue(rel.Class.Split('.').Last(), out var targetEntity))
        {
            string tableAlias = navName;
            string targetTable = targetEntity.Table;

            if (!joinFragments.Any(j => j.Contains(targetTable)))
                joinFragments.Add($", {targetTable} {tableAlias}");

            string fkColumn = rel.SourceColumn;
            string pkColumn = targetEntity.Properties.FirstOrDefault(p => p.IsPrimary)?.Column ?? "Id";
            string joinCondition = $"AND [{aliasPrefix}].{fkColumn} = {tableAlias}.{pkColumn}";

            if (!joinConditions.Contains(joinCondition))
                joinConditions.Add(joinCondition);

            var targetProp = targetEntity.Properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (targetProp != null)
            {
                string replacePattern = $@"\[{aliasPrefix}\]\.{navName}\.{propName}";
                hql = Regex.Replace(hql, replacePattern, $"{tableAlias}.{targetProp.Column}", RegexOptions.IgnoreCase);

                replacePattern = $@"{aliasPrefix}\.{navName}\.{propName}";
                hql = Regex.Replace(hql, replacePattern, $"{tableAlias}.{targetProp.Column}", RegexOptions.IgnoreCase);
            }
        }
    }

    // Insert joins before WHERE
    // Build FROM fragment
    if (joinFragments.Count > 0)
    {
        int whereIdx = hql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIdx > 0)
        {
            hql = hql.Insert(whereIdx, string.Join(" ", joinFragments) + " ");
        }
        else
        {
            hql += " " + string.Join(" ", joinFragments);
        }
    }

    // Build WHERE fragment
    if (joinConditions.Count > 0)
    {
        int whereIdx = hql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        string joinCond = string.Join(" AND ", joinConditions) + " AND ";

        if (whereIdx > 0)
        {
            hql = hql.Insert(whereIdx + 5, " " + joinCond); // Insert right after WHERE
        }
        else
        {
            hql += " WHERE " + joinCond;
        }
    }

    return hql;
}



foreach (var file in hbmFiles)
{
    var entity = HbmParser.Parse(file);
    if (entity == null) continue;
    allEntities.Add(entity);

    var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
    File.Copy(file, Path.Combine(outputPath, "Hbm", fileName + ".xml"), overwrite: true);
}

var entityMap = allEntities.ToDictionary(e => e.Name, e => e, StringComparer.OrdinalIgnoreCase);


foreach (var entity in allEntities)
{
    //XmlGenerator.ConvertToNativeSql(query.Value ?? "", entity.Name, entity.Table, entity).Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim(),
    foreach(var query in entity.Queries)
    {
        query.Sql = XmlGenerator.ConvertToNativeSql(query.Sql ?? "", 
            entity.Name, entity.Table, entity, entityMap
            );
    }
    File.WriteAllText(Path.Combine(outputPath, "Plans", entity.Name + ".json"), JsonConvert.SerializeObject(entity, Formatting.Indented));
}
