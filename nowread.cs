public static class AutoMapperProfilePerClassGenerator
{
    public static void Generate(List<EntityDefinition> entities, string[] domainClassNames, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var domainSet = new HashSet<string>(domainClassNames, StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            if (!domainSet.Contains(entity.Name))
                continue;

            string domainClass = $"Business.AMS.Domain.{entity.Name}.{entity.Name}";
            string entityClass = $"{entity.Namespace}.{entity.Name}";
            string profileClassName = $"{entity.Name}Profile";

            var sb = new StringBuilder();
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine();
            sb.AppendLine("namespace YourNamespace.MappingProfiles");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {profileClassName} : Profile");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {profileClassName}()");
            sb.AppendLine("        {");
            sb.AppendLine($"            CreateMap<{domainClass}, {entityClass}>()");

            foreach (var prop in entity.Properties)
            {
                if (!string.Equals(prop.Name, prop.Column, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"                .ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => src.{prop.Column}))");
                }
            }

            sb.AppendLine("                .ReverseMap();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputDir, $"{profileClassName}.cs"), sb.ToString());
        }
    }
}

string[] inputClasses = Console.ReadLine()?.Split(',') ?? Array.Empty<string>();
AutoMapperProfilePerClassGenerator.Generate(allEntities, inputClasses, Path.Combine(outputPath, "AutoMapperProfiles"));
