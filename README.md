// Entry point for your console utility
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace HbmToDapperConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputPath = "./HbmFiles";
            string outputPath = "./Output";
            var hbmFiles = Directory.GetFiles(inputPath, "*.hbm.xml");

            // Ensure output subfolders exist
            Directory.CreateDirectory(Path.Combine(outputPath, "POCOs"));
            Directory.CreateDirectory(Path.Combine(outputPath, "Plans"));
            Directory.CreateDirectory(Path.Combine(outputPath, "SQL"));
            Directory.CreateDirectory(Path.Combine(outputPath, "XML"));
            Directory.CreateDirectory(Path.Combine(outputPath, "Hbm"));
            Directory.CreateDirectory(Path.Combine(outputPath, "RepositoryLayer"));

            var allEntities = new List<EntityDefinition>();

            // Parse each HBM file and generate artifacts
            foreach (var file in hbmFiles)
            {
                var entity = HbmParser.Parse(file);
                if (entity == null) continue;
                allEntities.Add(entity);

                File.WriteAllText(Path.Combine(outputPath, "POCOs", entity.Name + ".cs"), PocoGenerator.Generate(entity));
                File.WriteAllText(Path.Combine(outputPath, "Plans", entity.Name + ".json"), JsonConvert.SerializeObject(entity, Formatting.Indented));
                File.WriteAllText(Path.Combine(outputPath, "SQL", entity.Name + ".sql"), SqlGenerator.Generate(entity));
                File.WriteAllText(Path.Combine(outputPath, "XML", entity.Name + ".xml"), XmlGenerator.Generate(entity));

                foreach (var query in entity.CustomQueries)
                {
                    var nativeSql = XmlGenerator.ConvertToNativeSql(query.Hql, entity.Name, entity.Table);
                    File.AppendAllText(Path.Combine(outputPath, "SQL", entity.Name + ".sql"), $"-- Named query: {query.Name}{nativeSql}");
                }

                // Copy original HBM
                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
                File.Copy(file, Path.Combine(outputPath, "Hbm", fileName + ".xml"), overwrite: true);
            }

            // Generate HTML metrics summary
            HtmlGenerator.Generate(hbmFiles, outputPath);

            // Generate repository layer based on many-to-one relationships
            RepositoryGenerator.GenerateRepositories(allEntities, Path.Combine(outputPath, "RepositoryLayer"));

            Console.WriteLine("Conversion completed.");
        }
    }

    public static class HbmParser
    {
        public static EntityDefinition Parse(string path)
        {
            var doc = XDocument.Load(path);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            var classElement = doc.Descendants(ns + "class").FirstOrDefault();
            if (classElement == null) return null;

            var entity = new EntityDefinition
            {
                Name = classElement.Attribute("name")?.Value?.Split('.').Last(),
                Table = classElement.Attribute("table")?.Value,
                IdColumn = classElement.Element(ns + "id")?.Attribute("column")?.Value,
                Properties = new List<PropertyDefinition>(),
                Relationships = new List<RelationshipDefinition>(),
                CustomQueries = new List<QueryDefinition>()
            };

            foreach (var prop in classElement.Elements(ns + "property"))
            {
                entity.Properties.Add(new PropertyDefinition
                {
                    Name = prop.Attribute("name")?.Value,
                    Column = prop.Attribute("column")?.Value,
                    Type = prop.Attribute("type")?.Value ?? "string"
                });
            }

            foreach (var rel in classElement.Elements().Where(e => e.Name.LocalName == "many-to-one" || e.Name.LocalName == "bag"))
            {
                var relDef = new RelationshipDefinition
                {
                    Name = rel.Attribute("name")?.Value,
                    Type = rel.Name.LocalName,
                    Column = rel.Element(ns + "key")?.Attribute("column")?.Value ?? rel.Attribute("column")?.Value
                };

                if (rel.Name.LocalName == "many-to-one")
                {
                    relDef.Class = rel.Attribute("class")?.Value?.Split('.').Last();
                }
                else if (rel.Name.LocalName == "bag")
                {
                    var oneToMany = rel.Element(ns + "one-to-many");
                    var manyToMany = rel.Element(ns + "many-to-many");

                    if (oneToMany != null)
                    {
                        relDef.InnerType = "one-to-many";
                        relDef.OneToManyClass = oneToMany.Attribute("class")?.Value?.Split('.').Last();
                    }
                    else if (manyToMany != null)
                    {
                        relDef.InnerType = "many-to-many";
                        relDef.ManyToManyClass = manyToMany.Attribute("class")?.Value?.Split('.').Last();
                    }
                }

                entity.Relationships.Add(relDef);
            }

            foreach (var query in doc.Descendants(ns + "query"))
            {
                entity.CustomQueries.Add(new QueryDefinition
                {
                    Name = query.Attribute("name")?.Value,
                    Hql = query.Value?.Trim()
                });
            }

            foreach (var query in doc.Descendants(ns + "sql-query"))
            {
                var name = query.Attribute("name")?.Value;
                var sqlText = query.Value?.Trim();

                if (!string.IsNullOrWhiteSpace(sqlText))
                {
                    entity.CustomQueries.Add(new QueryDefinition
                    {
                        Name = name,
                        Hql = sqlText
                    });
                }
            }

            return entity;
        }
    }

    public static class HtmlGenerator
    {
        public static void Generate(string[] hbmFiles, string outputPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><head><title>Dapper Conversion Metrics</title>");
            sb.AppendLine("<style>table { border-collapse: collapse; } th, td { border: 1px solid black; padding: 6px; white-space: pre-wrap; }</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h2>Dapper Conversion Summary</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>S.No</th><th>Filename</th><th>Total Queries</th><th>HQL Queries</th><th>SQL Queries</th><th>Total Bags</th><th>OneToMany</th>" +
                "<th>ManyToOne</th><th>ManyToMany</th>" +
                "<th>Relationship</th><th>Relationship Path Detail</th></tr>");

            var allEntities = hbmFiles.Select(HbmParser.Parse).Where(e => e != null).ToList();
            var relationshipMap = allEntities.Where(e => !string.IsNullOrWhiteSpace(e.Name)).ToDictionary(e => e.Name, e => e);

            int sno = 1;
            foreach (var entity in allEntities)
            {
                var (maxDepth, treePath) = GetAllPaths(entity, relationshipMap);

                var directRelationships = string.Join("<br/>", entity.Relationships
                    .Where(r => !string.IsNullOrWhiteSpace(r.Type))
                    .Select(r =>
                    {
                        string relType = r.InnerType ?? r.Type;
                        string className = r.Class ?? r.OneToManyClass ?? r.ManyToManyClass;
                        string columnName = r.Column ?? "";
                        string prefix = r.Type == "bag" ? "Bag-" : "Direct-";
                        return $"{prefix}{relType}:{className?.Split('.').LastOrDefault()} (Column: {columnName})";
                    }));

                int hqlCount = entity.CustomQueries.Count(q => !string.IsNullOrEmpty(q.Hql) && !q.Hql.TrimStart().ToLower().StartsWith("select * from"));
                int sqlCount = entity.CustomQueries.Count - hqlCount;

                int oneToMany = entity.Relationships.Count(r => (r.Type == "one-to-many") || (r.Type == "bag" && r.InnerType == "one-to-many"));
                int manyToOne = entity.Relationships.Count(r => (r.Type == "many-to-one") || (r.Type == "bag" && r.InnerType == "many-to-one"));
                int manyToMany = entity.Relationships.Count(r => (r.Type == "many-to-many") || (r.Type == "bag" && r.InnerType == "many-to-many"));
                int totalBags = entity.Relationships.Count(r => r.Type == "bag");

                sb.AppendLine("<tr>" +
                    $"<td>{sno++}</td>" +
                    $"<td>{entity.Name}</td>" +
                    $"<td>{entity.CustomQueries.Count}</td>" +
                    $"<td>{hqlCount}</td>" +
                    $"<td>{sqlCount}</td>" +
                    $"<td>{totalBags}</td>" +
                    $"<td>{oneToMany}</td>" +
                    $"<td>{manyToOne}</td>" +
                    $"<td>{manyToMany}</td>" +
                     //$"<td>{maxDepth}</td>" +
                     $"<td>{directRelationships}</td>" +
                    $"<td>{treePath}</td></tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(Path.Combine(outputPath, "metrics_summary.html"), sb.ToString());
        }

        private static (int, string) GetAllPaths(EntityDefinition entity, Dictionary<string, EntityDefinition> map)
        {
            var allPaths = new List<List<string>>();
            ExploreAllPaths(entity, map, new HashSet<string>(), new List<string>(), allPaths);

            int maxDepth = allPaths.Any() ? allPaths.Max(p => p.Count) - 1 : 0;
            string joined = string.Join("<br/>", allPaths.Select(p => string.Join("->", CompressPath(p))));
            return (maxDepth, joined);
        }

        private static void ExploreAllPaths(EntityDefinition entity, Dictionary<string, EntityDefinition> map, HashSet<string> visited, List<string> path, List<List<string>> allPaths)
        {
            if (string.IsNullOrWhiteSpace(entity?.Name) || visited.Contains(entity.Name)) return;

            path.Add(entity.Name);
            visited.Add(entity.Name);

            bool hasChild = false;
            foreach (var rel in entity.Relationships)
            {
                string targetClass = rel.Class ?? rel.OneToManyClass ?? rel.ManyToManyClass;
                string relType = rel.InnerType ?? rel.Type;
                string columnName = rel.Column ?? "";

                if (!string.IsNullOrWhiteSpace(targetClass))
                {
                    string simpleClassName = targetClass.Split('.').LastOrDefault();
                    string prefix = rel.Type == "bag" ? "Bag-" : "Direct-";
                    var label = prefix + relType + ":" + simpleClassName;
                    if (!string.IsNullOrEmpty(columnName))
                        label += " (Column: " + columnName + ")";

                    var newPath = new List<string>(path) { label };
                    if (map.TryGetValue(simpleClassName, out var child))
                    {
                        ExploreAllPaths(child, map, new HashSet<string>(visited), newPath, allPaths);
                        hasChild = true;
                    }
                    else
                    {
                        allPaths.Add(newPath);
                    }
                }
            }

            if (!hasChild)
            {
                allPaths.Add(new List<string>(path));
            }
        }

        private static List<string> CompressPath(List<string> path)
        {
            var result = new List<string>();
            var seen = new HashSet<string>();
            foreach (var entry in path)
            {
                string key = entry.Contains(":") ? entry.Split(':').Last().Split('(')[0].Trim() : entry;
                if (!seen.Contains(key))
                {
                    result.Add(entry);
                    seen.Add(key);
                }
            }
            return result;
        }

    }

    public static class PocoGenerator
    {
        public static string Generate(EntityDefinition entity)
        {
            var lines = new List<string>
            {
                "public class " + entity.Name,
                "{",
                $"    public string {entity.IdColumn} {{ get; set; }}"
            };

            // Generate properties for simple columns
            foreach (var prop in entity.Properties)
            {
                lines.Add($"    public {MapType(prop.Type)} {prop.Name} {{ get; set; }}");
            }

            // Generate properties for many-to-one foreign keys
            foreach (var rel in entity.Relationships.Where(r => r.Type == "many-to-one"))
            {
                if (!string.IsNullOrEmpty(rel.Column))
                {
                    // Use Column directly as property name
                    string fkProperty = rel.Column;
                    lines.Add($"    public virtual string {fkProperty} {{ get; set; }}");
                }
            }

            // Generate navigation properties
            foreach (var rel in entity.Relationships)
            {
                if (rel.Type == "bag")
                {
                    // One-to-many or many-to-many
                    string typeName = rel.InnerType == "one-to-many" ? rel.OneToManyClass : rel.ManyToManyClass;
                    lines.Add($"    public List<{typeName}> {rel.Name} {{ get; set; }}");
                }
                else if (rel.Type == "many-to-one")
                {
                    lines.Add($"    public virtual {rel.Class} {rel.Name} {{ get; set; }}");
                }
            }

            lines.Add("}");
            return string.Join("\n", lines);
        }

        private static string MapType(string hbmType)
        {
            return hbmType switch
            {
                "YesNo" => "bool",
                "StringClob" => "string",
                _ => "string"
            };
        }
    }

    public static class SqlGenerator
    {
        public static string Generate(EntityDefinition entity)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- SQL for entity: {entity.Name}");
            sb.AppendLine($"SELECT * FROM {entity.Table} WHERE {entity.IdColumn} = @{entity.IdColumn};");

            foreach (var rel in entity.Relationships)
            {
                if (rel.Type == "many-to-one")
                {
                    sb.AppendLine($"-- many-to-one: {rel.Name}");
                    sb.AppendLine($"SELECT * FROM {rel.Class} WHERE {rel.Column} = @{rel.Column};");
                }
                else if (rel.Type == "bag")
                {
                    sb.AppendLine($"-- bag: {rel.Name}");
                    sb.AppendLine($"SELECT * FROM {rel.Class} WHERE {rel.Column} = @{entity.IdColumn};");
                }
            }

            return sb.ToString();
        }
    }

    public static class XmlGenerator
    {
        public static string ConvertToNativeSql(string hql, string entityName, string tableName)
        {
            if (string.IsNullOrWhiteSpace(hql)) return hql;

            hql = hql.Trim();

            var selectDistinctRegex = new Regex($@"select\s+distinct\s+\w+\s+from\s+{entityName}\s+(\w+)", RegexOptions.IgnoreCase);
            hql = selectDistinctRegex.Replace(hql, m => $"SELECT DISTINCT * FROM {tableName} {m.Groups[1].Value}");

            var selectRegex = new Regex($@"select\s+\w+\s+from\s+{entityName}\s+(\w+)", RegexOptions.IgnoreCase);
            hql = selectRegex.Replace(hql, m => $"SELECT * FROM {tableName} {m.Groups[1].Value}");

            var fromRegex = new Regex($@"from\s+{entityName}\s+(\w+)", RegexOptions.IgnoreCase);
            hql = fromRegex.Replace(hql, m => $"SELECT * FROM {tableName} {m.Groups[1].Value}");

            hql = hql.Replace("left join", "LEFT JOIN").Replace("fetch", "");
            return hql.Trim();
        }

        public static string Generate(EntityDefinition entity)
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("entity",
                    new XAttribute("entity", entity.Name ?? string.Empty),
                    new XAttribute("table", entity.Table ?? string.Empty),
                    new XElement("id", new XAttribute("column", entity.IdColumn ?? string.Empty)),
                    new XElement("properties",
                        entity.Properties.Select(p =>
                            new XElement("property",
                                new XAttribute("name", p.Name ?? string.Empty),
                                new XAttribute("column", p.Column ?? string.Empty),
                                new XAttribute("type", p.Type ?? "string")
                            ))
                    ),
                    new XElement("relationships",
                        entity.Relationships.Select(r =>
                            new XElement("relationship",
                                new XAttribute("name", r.Name ?? string.Empty),
                                new XAttribute("type", r.Type ?? string.Empty),
                                new XAttribute("class", r.Class ?? string.Empty),
                                new XAttribute("column", r.Column ?? string.Empty)
                            ))
                    ),
                    new XElement("queries",
                        entity.CustomQueries.Select(q =>
                        {
                            var paramElements = new List<XElement>();
                            var namedParams = Regex.Matches(q.Hql ?? string.Empty, @"[:@]([a-zA-Z_][a-zA-Z0-9_]*)");
                            foreach (Match match in namedParams)
                            {
                                var value = match.Groups[1].Value;
                                if (!string.IsNullOrWhiteSpace(value) && !paramElements.Any(p => p.Attribute("name")?.Value == value))
                                {
                                    string type = value.ToLower().Contains("id") ? "guid" : "string";
                                    paramElements.Add(new XElement("param",
                                        new XAttribute("name", value),
                                        new XAttribute("type", type)
                                    ));
                                }
                            }

                            int questionCount = Regex.Matches(q.Hql ?? string.Empty, "\\?").Count;
                            for (int i = 1; i <= questionCount; i++)
                            {
                                paramElements.Add(new XElement("param",
                                    new XAttribute("name", $"param{i}"),
                                    new XAttribute("type", "string")));
                            }

                            return new XElement("query",
                                new XAttribute("name", q.Name ?? string.Empty),
                                new XElement("sql", ConvertToNativeSql(q.Hql, entity.Name, entity.Table)),
                                paramElements.Any() ? new XElement("parameters", paramElements) : null
                            );
                        })
                    )
                )
            );

            return doc.ToString();
        }

    }

    public static class RepositoryGenerator
    {
        public static void GenerateRepositories(List<EntityDefinition> entities, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            foreach (var parent in entities)
            {
                // Identify all many-to-one relationships in parent
                var children = parent.Relationships
                    .Where(r => r.Type == "many-to-one" || r.InnerType == "many-to-one")
                    .Select(r => new { ChildName = r.Class ?? r.OneToManyClass ?? r.ManyToManyClass, ForeignKey = r.Column })
                    .ToList();

                if (!children.Any())
                    continue; // skip if no many-to-one

                string repoClassName = parent.Name + "Repository";
                var sb = new StringBuilder();
                sb.AppendLine("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Threading.Tasks;\nusing YourNamespace.Helpers;\nusing YourNamespace.Models;\nusing YourNamespace.QueryLoader;\n");
                sb.AppendLine("namespace YourNamespace.RepositoryLayer");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {repoClassName} : BaseRepository");
                sb.AppendLine("    {");
                sb.AppendLine("        private readonly IXmlQueryLoader _queryLoader;\n");
                sb.AppendLine($"        public {repoClassName}(IXmlQueryLoader queryLoader)" + "");
                sb.AppendLine("        {\n            _queryLoader = queryLoader;\n        }\n");

                string parentPlural = parent.Name + "s"; // naive pluralization
                sb.AppendLine($"        public async Task<IEnumerable<{parent.Name}>> FindAll{parentPlural}Async()\n        {{");
                sb.AppendLine("            // Load parent query");
                sb.AppendLine($"            QueryDefinition queryDef = _queryLoader.GetQuery(nameof({parent.Name}), QueryMapConstants.{parent.Name}Constants.FindAll{parentPlural});");
                sb.AppendLine($"            List<{parent.Name}> results = await base.QueryAsync<{parent.Name}>(queryDef?.Sql);\n");

                foreach (var child in children)
                {
                    sb.AppendLine($"            // Prepare keys from parent to fetch {child.ChildName}\n");
                    sb.AppendLine($"            List<string> keys = results.Select(r => r.{child.ForeignKey}).Distinct().ToList();\n");
                    sb.AppendLine($"            QueryDefinition childQuery = _queryLoader.GetQuery(nameof({child.ChildName}), QueryMapConstants.{child.ChildName}Constants.FindBy{parent.Name}Keys);\n");
                    sb.AppendLine($"            List<{child.ChildName}> childEntities = await QueryAsync<{child.ChildName}>(childQuery?.Sql, new {{ {child.ChildName}Keys = keys }});\n");
                    sb.AppendLine($"            NavigationAssignmentHelper.AssignNavigation<{parent.Name}, {child.ChildName}, string>(\n                results,\n                childEntities,\n                p => p.{child.ForeignKey},\n                c => c.{child.ChildName}Key,\n                (p, c) => p.{child.ChildName} = c\n            );\n");
                }

                sb.AppendLine("            return results;\n        }");
                sb.AppendLine("    }\n}");

                File.WriteAllText(Path.Combine(outputFolder, repoClassName + ".cs"), sb.ToString());
            }
        }
    }

    public class EntityDefinition
    {
        public string Name { get; set; }
        public string Table { get; set; }
        public string IdColumn { get; set; }
        public List<PropertyDefinition> Properties { get; set; }
        public List<RelationshipDefinition> Relationships { get; set; }
        public List<QueryDefinition> CustomQueries { get; set; }
    }

    public class PropertyDefinition
    {
        public string Name { get; set; }
        public string Column { get; set; }
        public string Type { get; set; }
    }

    public class RelationshipDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }  // bag, many-to-one, one-to-many, etc.
        public string InnerType { get; set; } // one-to-many or many-to-many inside bag
        public string Class { get; set; } // fallback
        public string OneToManyClass { get; set; }
        public string ManyToManyClass { get; set; }
        public string Column { get; set; }
    }

    public class QueryDefinition
    {
        public string Name { get; set; }
        public string Hql { get; set; }
    }
}
