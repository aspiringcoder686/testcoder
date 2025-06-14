Directory.CreateDirectory(Path.Combine(outputPath, "Constants"));
var constantsCode = ConstantGenerator.GenerateConstants(allEntities);
File.WriteAllText(Path.Combine(outputPath, "Constants", "QueryMapConstants.cs"), constantsCode);



public static class ConstantGenerator
{
    public static string GenerateConstants(List<EntityDefinition> entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace YourNamespace.Constants");
        sb.AppendLine("{");
        sb.AppendLine("    public static class QueryMapConstants");
        sb.AppendLine("    {");

        foreach (var entity in entities)
        {
            sb.AppendLine($"        public static class {entity.Name}Constants");
            sb.AppendLine("        {");

            var uniqueQueries = entity.Queries
                .GroupBy(q => q.Name)
                .Select(g => g.First());

            foreach (var query in uniqueQueries)
            {
                string constName = NormalizeName(query.Name);
                sb.AppendLine($"            public const string {constName} = \"{query.Name}\";");
            }

            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        // Convert to PascalCase, remove invalid chars if needed
        return name.Replace(".", "")
                   .Replace("-", "")
                   .Replace(" ", "")
                   .Replace("_", "")
                   .Replace("(", "")
                   .Replace(")", "");
    }
}
