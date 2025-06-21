// Split HQL into preWhere and whereClause
int whereIndex = hql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
string preWhere = whereIndex >= 0 ? hql.Substring(0, whereIndex + 5) : hql;  // include "WHERE"
string whereClause = whereIndex >= 0 ? hql.Substring(whereIndex + 5) : "";   // after "WHERE"

foreach (var rel in entity.Relationships)
{
    if (!string.IsNullOrWhiteSpace(rel.Name) && !string.IsNullOrWhiteSpace(rel.SourceColumn))
    {
        // Replace only when it's a standalone property, not part of navigation (no trailing dot)

        string patternWithAlias = $@"\b{alias}\.{Regex.Escape(rel.Name)}\b(?!\.)";
        whereClause = Regex.Replace(whereClause, patternWithAlias, $"[{alias}].{rel.SourceColumn}", RegexOptions.IgnoreCase);

        string patternNoAlias = $@"\b{Regex.Escape(rel.Name)}\b(?!\.)";
        whereClause = Regex.Replace(whereClause, patternNoAlias, rel.SourceColumn, RegexOptions.IgnoreCase);
    }
}

// Combine preWhere + updated whereClause
hql = preWhere + " " + whereClause;
