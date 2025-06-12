public static string ConvertToNativeSql(string hql, string entityName, string tableName, EntityDefinition entity)
    {
        if (string.IsNullOrWhiteSpace(hql)) return hql;

        hql = hql.Trim();

        var aliasPattern = new Regex($"from\\s+{entityName}\\s+(\\w+)", RegexOptions.IgnoreCase);
        var aliasMatch = aliasPattern.Match(hql);
        string alias = aliasMatch.Success ? aliasMatch.Groups[1].Value : entityName;
        string aliasSql = $"[{alias}]";
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

        Regex paramPattern = new(@"(\b[\w]+\.)?(\w+)\s*(=|<>|!=|>=|<=|>|<)\s*\?", RegexOptions.IgnoreCase);
        hql = paramPattern.Replace(hql, match =>
        {
            string property = match.Groups[2].Value;
            string fullMatch = match.Groups[0].Value;
            return fullMatch.Replace("?", "@" + property);
        });

        Regex likePattern = new(@"(\b[\w]+\.)?(\w+)\s+like\s+\?", RegexOptions.IgnoreCase);
        hql = likePattern.Replace(hql, match =>
        {
            string property = match.Groups[2].Value;
            return match.Value.Replace("?", "@" + property);
        });

        Regex betweenPattern = new(@"(\b[\w]+\.[\w]+)\s+between\s+\?\s+and\s+\?", RegexOptions.IgnoreCase);
        hql = betweenPattern.Replace(hql, match =>
        {
            string leftSide = match.Groups[1].Value;
            return $"{leftSide} between @Min and @Max";
        });

        if (!string.IsNullOrWhiteSpace(alias))
        {
            var aliasRefPattern = new Regex($"\\b{alias}\\.(\\w+)", RegexOptions.IgnoreCase);
            hql = aliasRefPattern.Replace(hql, m => $"[{alias}].{m.Groups[1].Value}");
        }

        foreach (var prop in entity.Properties)
        {
            if (!string.IsNullOrWhiteSpace(prop.Name) && !string.IsNullOrWhiteSpace(prop.Column) &&
                !prop.Name.Equals(prop.Column, StringComparison.OrdinalIgnoreCase))
            {
                string propPattern = $"\\[{alias}\\]\\.{Regex.Escape(prop.Name)}\\b";
                hql = Regex.Replace(hql, propPattern, $"[{alias}].{prop.Column}", RegexOptions.IgnoreCase);
            }
        }

        foreach (var rel in entity.Relationships)
        {
            if (!string.IsNullOrWhiteSpace(rel.Name) && !string.IsNullOrWhiteSpace(rel.Column))
            {
                string relPattern = $"\\[{alias}\\]\\.{Regex.Escape(rel.Name)}\\.Id";
                hql = Regex.Replace(hql, relPattern, $"[{alias}].{rel.Column}", RegexOptions.IgnoreCase);
                hql = Regex.Replace(hql, $"@{Regex.Escape(rel.Name)}\\.Id\\b", $"@{rel.Column}", RegexOptions.IgnoreCase);
            }
        }

        hql = hql.Replace("\n", " ").Replace("\r", " ").Trim();
        return hql;
    }
