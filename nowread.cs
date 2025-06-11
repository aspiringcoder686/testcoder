    public static string ConvertToNativeSql(string hql, string entityName, string tableName, EntityDefinition entity)
    {
        if (string.IsNullOrWhiteSpace(hql)) return hql;

        hql = hql.Trim();

        // Step 1: Detect alias
        var aliasPattern = new Regex($@"from\s+{entityName}\s+(\w+)", RegexOptions.IgnoreCase);
        var aliasMatch = aliasPattern.Match(hql);
        string alias = aliasMatch.Success ? aliasMatch.Groups[1].Value : entityName;
        string aliasSql = $"[{alias}]";

        string fromClause = $"FROM {tableName} {aliasSql}";

        // Step 2: Inject SELECT * if missing
        if (hql.StartsWith("from", StringComparison.OrdinalIgnoreCase))
        {
            hql = Regex.Replace(hql, $@"from\s+{entityName}(\s+\w+)?", fromClause, RegexOptions.IgnoreCase);
            hql = "SELECT * " + hql;
        }
        else
        {
            hql = Regex.Replace(hql, $@"select\s+(distinct\s+)?\w+\s+from\s+{entityName}\s+\w+", $"SELECT $1* {fromClause}", RegexOptions.IgnoreCase);
        }

        // Step 3: Remove 'fetch'
        hql = hql.Replace("fetch", "", StringComparison.OrdinalIgnoreCase);

        // Step 4: Replace ? with @ColumnName
        Regex paramPattern = new(@"(\b[\w]+\.)?(\w+)\s*(=|<>|!=|>=|<=|>|<)\s*\?", RegexOptions.IgnoreCase);
        hql = paramPattern.Replace(hql, match =>
        {
            string columnName = match.Groups[2].Value;
            return match.Value.Replace("?", "@" + columnName);
        });

        // Step 5: Format alias references with brackets
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var aliasRefPattern = new Regex($@"\b{alias}\.(\w+)\b", RegexOptions.IgnoreCase);
            hql = aliasRefPattern.Replace(hql, m => $"[{alias}].{m.Groups[1].Value}");
        }

        // Step 6: Replace [alias].Property with mapped [alias].Column
        foreach (var prop in entity.Properties)
        {
            if (!string.IsNullOrWhiteSpace(prop.Name) && !string.IsNullOrWhiteSpace(prop.Column) &&
                !prop.Name.Equals(prop.Column, StringComparison.OrdinalIgnoreCase))
            {
                string propPattern = $@"\[{alias}\]\.{Regex.Escape(prop.Name)}\b";
                hql = Regex.Replace(hql, propPattern, $"[{alias}].{prop.Column}", RegexOptions.IgnoreCase);
            }
        }

        // Step 7: Normalize whitespace
        hql = hql.Replace("\n", " ").Replace("\r", " ").Trim();

        return hql;
    }

}
