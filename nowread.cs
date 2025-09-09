         string[] inputClasses = ["Asset"];
         //AutoMapperProfilePerClassGenerator.Generate(allEntities, inputClasses, Path.Combine(outputPath, "AutoMapperProfiles"));

         string outputDir = Path.Combine(
 AppDomain.CurrentDomain.BaseDirectory,
 "Generated",
 "Projections");

         // 3. Path to your EF entities (.cs files under NowBet.AMS.DataAccess\Entities)
         string efEntitiesFolder = @"D:\0-AMS\Automation\Tool\NowBet.AMS.DataAccess\Entities";

         // 4. Generate projection files
         ProjectionGenerator.Generate(allEntities, outputDir, efEntitiesFolder);

         string queryoutputDir = Path.Combine(
AppDomain.CurrentDomain.BaseDirectory,
"Generated",
"QueryGenerated");

         QueryGenerator.Generate_old(allEntities, queryoutputDir, efEntitiesFolder);


using System.Text;
using NowBet.AMS.AdminAPI.Repositories.Models;
using NowBet.AMS.DataAccess.Contexts.EF;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Automation.Common.Utility.Generators
{
    public static class AutoMapperProfilePerClassGenerator
    {

        public static void Generate(List<EntityDefinition> entities, string[] domainClassNames, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            var domainSet = new HashSet<string>(domainClassNames, StringComparer.OrdinalIgnoreCase);
            string filePath = @"D:\0-AMS\Automation\Tool\NowBet.AMS.DataAccess\Entities";
            foreach (var entity in entities)
            {
                //if (!domainSet.Contains(entity.Name))
                //    continue;

                string domainClass = $"{entity.Name}";
                string entityClass = $"{MakePascalCase(filePath,entity.Table)}";
                
                string profileClassName = $"{entity.Name}Profile";
                string entityPath = filePath + "\\" + entityClass + ".cs";
                var sb = new StringBuilder();
                sb.AppendLine("using AutoMapper;");

                sb.AppendLine("using NowBet.AMS.DataAccess.Entities;");
                sb.AppendLine("using NowBet.AMS.Domain.Entities;");

                sb.AppendLine();
                sb.AppendLine("namespace NowBet.AMS.Shared.Mapper.Automapper");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {profileClassName} : Profile");
                sb.AppendLine("    {");
                sb.AppendLine($"        public {profileClassName}()");
                sb.AppendLine("        {");
                sb.AppendLine($"            CreateMap<{entityClass}, {domainClass}>()");

                entity.EntityClassName = entityClass;

                // Map primitive properties
                foreach (var prop in entity.Properties)
                {
                    string expectedProperty = prop.Column.Replace("_","");
                    string targetType = "";




                    if (!string.Equals(prop.Name, prop.Column, StringComparison.OrdinalIgnoreCase))
                    {
                        string? match = RoslynHelper.GetMatchingPropertyFromClass(entityPath, expectedProperty, targetType);
                        if (match != null)
                        {
                            sb.AppendLine($"                .ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => src.{match}))");
                        }
                        else
                        {
                            sb.AppendLine($"//*Placeholder--Starts");
                            sb.AppendLine($"//                .ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => src.{prop.Column}))");
                            sb.AppendLine($"//*Placeholder--Ends");
                        }
                    }
                }

                // Map many-to-one relationships
                //if (entity.Relationships != null)
                //{
                //    foreach (var rel in entity.Relationships.Where(r => r.Type == "many-to-one"))
                //    {
                //        // Check if the domain might expose the related name as a flattened string
                //        // For now just assume direct mapping (e.g., src.RelatedEntity => dest.RelatedEntity)
                //        sb.AppendLine($"                .ForMember(dest => dest.{rel.Name}, opt => opt.MapFrom(src => src.{rel.Name}))");
                //    }
                //}

                foreach (var rel in entity.Relationships)//.Where(r => r.Type == "many-to-one"))
                {
                    // Get the name of the referenced entity
                    var relatedEntity = entities.FirstOrDefault(e => e.Name == rel.Class?.Split('.').Last());

                    if (relatedEntity != null)
                    {
                        // Get its table name (e.g., AM_User)
                        string tableName = relatedEntity.Table;
                        string srcProperty = MakePascalCase(filePath,tableName);
                        string destProperty = rel.Name;

                        string expectedProperty = destProperty;
                        string targetType = srcProperty;

                        string? match = RoslynHelper.GetMatchingPropertyFromClass(entityPath, expectedProperty, targetType);

                        if (match != null)
                        {
                            sb.AppendLine($"                .ForMember(dest => dest.{destProperty}, opt => opt.MapFrom(src => src.{match}))");
                        }
                        else
                        {
                            sb.AppendLine($"//*Placeholder--Starts");
                            sb.AppendLine($"//                .ForMember(dest => dest.{destProperty}, opt => opt.MapFrom(src => src.{targetType})) ");
                            sb.AppendLine($"//*Placeholder--Ends");
                        }


                        
                    }
                }

                sb.AppendLine("                .ReverseMap();");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDir, $"{profileClassName}.cs.txt"), sb.ToString());

                //var code = GenerateIncludeQuery(entity, entity.Name);

               // File.WriteAllText(Path.Combine(outputDir, "1-include", $"{entity.Name}.include.cs.txt"), code);

                //IncludeGenerator.GenerateIncludeFile(typeof(AMSDbContext),  entity.EntityClassName, maxDepth: 3, dtoTypeName: "AssetDto" , outputdr: Path.Combine(outputDir , "1-include"), domainName:entity.Name);
            }

            var sb1 = new StringBuilder();
            sb1.AppendLine("Sno,hbmfile,Entityname,domainname,tablename,Properties,CompositeKey,Components,Relationships,Queries,Joins");
            int sno = 1;
            foreach (var entity in entities)
            {
                string hbmfile = $"{entity.Name}.hbm.xml";
                string entityname = entity.EntityClassName;
                string domainname = entity.Name;
                string tablename = entity.Table;

                int properties = entity.Properties.Count ;
                int compositeKey = entity.CompositeKey==null? 0 : entity.CompositeKey.Count ;
                int components =  entity.Components == null ? 0 : entity.Components.Count;
                int relationships = entity.Relationships.Count;
                int queries =  entity.Queries.Count;
                int joins = entity.Joins.Count;

                sb1.AppendLine($"{sno},{hbmfile},{entityname},{domainname},{tablename},{properties},{compositeKey},{components},{relationships},{queries},{joins}");

                sno++;
            }
            File.WriteAllText(Path.Combine(outputDir, $"table-metrics.csv"), sb1.ToString());


        }


        public static string GenerateIncludeQuery(EntityDefinition entity, string domainClassName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"var result = await securedEntityManger.Find<{entity.EntityClassName}>(");
            sb.AppendLine("    q => q");

            // Example Where clause (customize as needed)
            sb.AppendLine("        .Where(p => /* your conditions here */)");

            // Add .Include for each relationship
            foreach (var rel in entity.Relationships ?? new List<RelationshipDefinition>())
            {
                sb.AppendLine($"        .Include(r => r.{rel.Name})");
            }

            sb.AppendLine("        .OrderBy(r => r.<OrderProperty>)");
            sb.AppendLine("        .AsNoTracking()");
            sb.AppendLine("        .AsSplitQuery()");
            sb.AppendLine(");");

            sb.AppendLine();
            sb.AppendLine($"// Map to domain object");
            sb.AppendLine($"var {domainClassName.ToLower()} = mapper.Map<{domainClassName}>(result);");

            return sb.ToString();
        }

        private static string MakePascalCase(string filePath,string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
            {

                if (tableName.StartsWith("AM_", StringComparison.OrdinalIgnoreCase))
                {
                    // Replace only the starting "AM_" → "Am"
                    tableName = "Am" + tableName.Substring(3);  // Skip "AM_"
                }
                tableName = tableName.Replace("_", "");
                tableName = FindFileNameIgnoreCase(filePath, tableName);
                tableName = tableName.Replace(".cs", "");
                // Convert AM_User → AmUser (PascalCase)
                //return string.Concat(
                //    tableName.Split('_', StringSplitOptions.RemoveEmptyEntries)
                //             .Select(part => char.ToUpper(part[0]) + part.Substring(1).ToLower())
                //);
            }
            return tableName;
        }

        public static string? FindFileNameIgnoreCase(string folder, string name)
        {
            string target = name + ".cs";

            foreach (var file in Directory.GetFiles(folder, "*.cs"))
            {
                if (string.Equals(Path.GetFileName(file), target, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFileName(file); // ✅ Return just the filename
            }

            return name;
        }
    }


    internal static class RoslynHelper
    {
        /// <summary>
        /// Try to resolve a property name on an EF entity class file.
        /// - preferTypeMatch: first look for a property whose type matches 'typeName', then '<expectedName>Navigation', then exact name.
        /// </summary>
        public static string? GetMatchingPropertyFromClass(string filePath, string expectedPropertyName, string typeName, bool preferTypeMatch = false)
        {
            if (!File.Exists(filePath)) return null;

            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode == null) return null;

            var props = classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>();

            if (preferTypeMatch)
            {
                foreach (var p in props)
                {
                    string pType = GetObjectName(p);
                    if (!string.IsNullOrEmpty(typeName) &&
                        pType.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        return p.Identifier.Text;
                }
                string navName = expectedPropertyName + "Navigation";
                foreach (var p in props)
                {
                    if (p.Identifier.Text.Equals(navName, StringComparison.OrdinalIgnoreCase))
                        return p.Identifier.Text;
                }
            }

            foreach (var p in props)
            {
                if (p.Identifier.Text.Equals(expectedPropertyName, StringComparison.OrdinalIgnoreCase))
                    return p.Identifier.Text;
            }

            foreach (var p in props)
            {
                string pType = GetObjectName(p);
                if (!string.IsNullOrEmpty(typeName) &&
                    pType.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    return p.Identifier.Text;
            }

            return null;
        }

        private static string GetObjectName(PropertyDeclarationSyntax prop)
        {
            string t = prop.Type.ToString().Replace("?", "");
            int s = t.IndexOf('<');
            int e = t.IndexOf('>');
            if (s != -1 && e != -1 && e > s)
            {
                t = t.Substring(s + 1, e - s - 1);
            }
            return t;
        }
    }
}

