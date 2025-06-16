public static string ConvertToNativeSql(string hql, string entityName, string tableName, EntityDefinition entity)
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

// Helper to check if alias is a SQL keyword
private static bool IsSqlKeyword(string word)
{
    string[] keywords = { "where", "from", "select", "and", "or", "order", "group", "having" };
    return keywords.Contains(word, StringComparer.OrdinalIgnoreCase);
}

// Regex patterns
[GeneratedRegex(@"(\b[\w]+\.)?(\w+)\s*(=|<>|!=|>=|<=|>|<)\s*\?", RegexOptions.IgnoreCase, "en-US")]
private static partial Regex MyRegex2();

[GeneratedRegex(@"(\b[\w]+\.)?(\w+)\s+like\s+\?", RegexOptions.IgnoreCase, "en-US")]
private static partial Regex MyRegex3();

[GeneratedRegex(@"(\b[\w]+\.[\w]+)\s+between\s+\?\s+and\s+\?", RegexOptions.IgnoreCase, "en-US")]
private static partial Regex MyRegex4();
