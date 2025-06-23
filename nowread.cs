using System.Xml.Linq;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace SpringXmlEnhancedConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Spring.NET XML Enhanced Converter");

            string xmlPath = args.Length > 0 ? args[0] : "spring.config.xml";
            if (!File.Exists(xmlPath))
            {
                Console.WriteLine($"❌ File not found: {xmlPath}");
                return;
            }

            XDocument doc = XDocument.Load(xmlPath);

            var appsettings = new Dictionary<string, object>();
            var diRegistrations = new StringBuilder();
            var dependencyGraph = new StringBuilder();
            var unsupported = new StringBuilder();
            var placeholders = new HashSet<string>();

            // Handle db:provider
            var connStrings = new Dictionary<string, string>();
            foreach (var provider in doc.Descendants().Where(x => x.Name.LocalName == "provider"))
            {
                var id = provider.Attribute("id")?.Value;
                var connStr = provider.Attribute("connectionString")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(connStr))
                {
                    connStrings[id] = connStr;
                    ExtractPlaceholders(connStr, placeholders);
                }
            }
            if (connStrings.Count > 0)
                appsettings["ConnectionStrings"] = connStrings;

            // Handle objects
            foreach (var obj in doc.Descendants().Where(x => x.Name.LocalName == "object"))
            {
                var id = obj.Attribute("id")?.Value;
                var type = obj.Attribute("type")?.Value;
                var singleton = obj.Attribute("singleton")?.Value ?? "true";

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                {
                    diRegistrations.AppendLine($"// {id}: {type}");
                    diRegistrations.AppendLine($"services.Add{(singleton == "true" ? "Singleton" : "Scoped")}<{type}>();");
                }

                var deps = new List<string>();

                foreach (var arg in obj.Elements().Where(x => x.Name.LocalName == "constructor-arg"))
                {
                    var refVal = arg.Attribute("ref")?.Value;
                    if (!string.IsNullOrEmpty(refVal))
                        deps.Add(refVal);

                    var valVal = arg.Attribute("value")?.Value;
                    if (!string.IsNullOrEmpty(valVal))
                        ExtractPlaceholders(valVal, placeholders);
                }

                foreach (var prop in obj.Elements().Where(x => x.Name.LocalName == "property"))
                {
                    var name = prop.Attribute("name")?.Value;
                    var val = prop.Attribute("value")?.Value;
                    var refVal = prop.Attribute("ref")?.Value;

                    if (!string.IsNullOrEmpty(val))
                    {
                        ExtractPlaceholders(val, placeholders);
                        AddToAppSettings(appsettings, id, name, val);
                    }

                    if (!string.IsNullOrEmpty(refVal))
                    {
                        deps.Add(refVal);
                    }

                    // Handle list/dictionary
                    if (prop.Elements().Any(x => x.Name.LocalName == "dictionary"))
                    {
                        var dict = new Dictionary<string, string>();
                        foreach (var entry in prop.Descendants().Where(x => x.Name.LocalName == "entry"))
                        {
                            var key = entry.Attribute("key")?.Value;
                            var entryVal = entry.Attribute("value")?.Value;
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(entryVal))
                                dict[key] = entryVal;
                        }
                        AddToAppSettings(appsettings, id, name, dict);
                    }
                    else if (prop.Elements().Any(x => x.Name.LocalName == "list"))
                    {
                        var list = prop.Descendants().Where(x => x.Name.LocalName == "value").Select(v => v.Value).ToList();
                        AddToAppSettings(appsettings, id, name, list);
                    }
                }

                if (deps.Count > 0)
                {
                    dependencyGraph.AppendLine($"{id}");
                    foreach (var dep in deps)
                        dependencyGraph.AppendLine($"  -> {dep}");
                }
            }

            // Handle unsupported tags
            if (doc.Descendants().Any(x => x.Name.LocalName == "attribute-driven"))
                unsupported.AppendLine("<tx:attribute-driven> not supported; use TransactionScope or NHibernate BeginTransaction.");
            if (doc.Descendants().Any(x => x.Name.LocalName == "parser"))
                unsupported.AppendLine("<parser> not required in .NET 8; config is code-based.");
            if (doc.Descendants().Any(x => x.Name.LocalName == "resource"))
                unsupported.AppendLine("<resource> config includes must be merged manually.");

            // Output files
            File.WriteAllText("appsettings.generated.json", JsonSerializer.Serialize(appsettings, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("✅ appsettings.generated.json written.");

            File.WriteAllText("DIRegistration.generated.cs", WrapDI(diRegistrations.ToString()));
            Console.WriteLine("✅ DIRegistration.generated.cs written.");

            File.WriteAllText("DependencyGraph.generated.txt", dependencyGraph.ToString());
            Console.WriteLine("✅ DependencyGraph.generated.txt written.");

            File.WriteAllText("UnsupportedFeatures.txt", unsupported.ToString());
            Console.WriteLine("✅ UnsupportedFeatures.txt written.");
        }

        static void ExtractPlaceholders(string input, HashSet<string> placeholders)
        {
            var matches = Regex.Matches(input ?? "", @"\$\{(.*?)\}");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                    placeholders.Add(match.Groups[1].Value);
            }
        }

        static void AddToAppSettings(Dictionary<string, object> appsettings, string objId, string propName, object value)
        {
            if (!appsettings.ContainsKey(objId))
                appsettings[objId] = new Dictionary<string, object>();

            var section = appsettings[objId] as Dictionary<string, object>;
            section[propName] = value;
        }

        static string WrapDI(string body)
        {
            return @$"
using Microsoft.Extensions.DependencyInjection;

public static class DIRegistration
{{
    public static void RegisterServices(IServiceCollection services)
    {{
{body}
    }}
}}";
        }
    }
}
