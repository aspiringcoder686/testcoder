using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace UnassignAnalyzer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // 🔧 Set your source root here
            string root = @"D:";

            // 🔧 Optional external NHibernate mapping directory (your example: D:\mapping)
            string mappingDir = @"D:\"; // leave as "" if you don't want to use it

            // 🔧 Dynamic URL patterns (each line may contain multiple fragments separated by ';')
            var urls = new[]
            {
                
            };

            // 📦 Output holders (treated as directories; per-endpoint files will be written inside)
            args = new[]
            {
                "--root", root,
                "--mapping-dir", mappingDir,
                "--depth", "3",
                "--out-json", "analysis8\\json\\placeholder.json",
                "--out-md",   "analysis8\\md\\placeholder.md",
                "--out-html", "analysis8\\html\\placeholder.html"
            };

            var options = Options.Parse(args);
            if (options.ShowHelp)
            {
                Options.PrintHelp();
                return 0;
            }

            if (!Directory.Exists(options.Root))
            {
                Console.Error.WriteLine($"Root not found: {options.Root}");
                return 2;
            }

            try
            {
                var indexer = new SourceIndexer(options.Root, options.FileGlob);
                indexer.Build();

                var analyzer = new Analyzer(indexer, options.MaxDepth);
                var printer = new ReportPrinter();

                // Stop traversal (repository/security managers)
                var stopClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "AmsSecurityManager",
                    "SecureEntityManager"
                };

                // 🚫 Ignore calls everywhere (do not add edges / not shown)
                Func<Analyzer.CallCandidate, bool> ignoreCall = IgnoreRules;

                // NHibernate named query index (from --mapping-dir and under --root)
                var namedIndex = NamedQueryResolver.Build(options.Root, options.MappingDir);

                // Parse (controller, method) targets from dynamic URLs
                var targets = EnumerateTargetsFromUrls(urls)
                              .Distinct(StringTupleComparer.OrdinalIgnoreCase)
                              .ToList();

                if (targets.Count == 0)
                {
                    Console.WriteLine("No valid controller/method pairs found in the provided URLs.");
                    return 0;
                }

                // Ensure output directories exist
                var jsonDir = EnsureOutDirectory(options.OutJson);
                var mdDir = EnsureOutDirectory(options.OutMarkdown);
                var htmlDir = EnsureOutDirectory(options.OutHtml);

                // Consolidated (all endpoints)
                var consolidated = new ConsolidatedMetrics { Endpoints = new List<EndpointMetrics>() };

                foreach (var (controller, method) in targets)
                {
                    // Find controller code-behind file anywhere
                    var controllerFile = Directory.EnumerateFiles(options.Root, controller + ".aspx.cs", SearchOption.AllDirectories)
                                                  .FirstOrDefault();

                    if (controllerFile == null)
                    {
                        Console.WriteLine($"❌ Controller file not found for '{controller}'");

                        // Record NotFound endpoint (Very High complexity) + stub HTML
                        var safeNameNF = SafeFileName($"{controller}_{method}");
                        var endpointNF = new EndpointMetrics
                        {
                            Controller = controller,
                            Method = method,
                            ControllerFile = controller + ".aspx.cs",
                            Complexity = "Very High",
                            NotFound = true,
                            OutputHtml = safeNameNF + ".html"
                        };
                        consolidated.Endpoints.Add(endpointNF);

                        if (!string.IsNullOrWhiteSpace(htmlDir))
                        {
                            var nfHtml = HtmlReportPrinter.BuildNotFoundPage($"{controller}.{method}", endpointNF.ControllerFile);
                            File.WriteAllText(Path.Combine(htmlDir, safeNameNF + ".html"), nfHtml, Encoding.UTF8);
                        }
                        continue;
                    }

                    // Find method
                    var rootMethod = indexer.FindMethodInFile(controllerFile, method);
                    if (rootMethod == null)
                    {
                        var candidates = indexer.FindMethodsByName(method);
                        rootMethod = candidates
                            .OrderByDescending(m => m.FilePath.EndsWith(".aspx.cs", StringComparison.OrdinalIgnoreCase))
                            .ThenByDescending(m => m.FilePath.IndexOf(controller, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ThenBy(m => m.StartLine)
                            .FirstOrDefault();
                    }

                    if (rootMethod == null)
                    {
                        Console.WriteLine($"❌ Could not find method '{method}' for controller '{controller}'");

                        var safeNameNF = SafeFileName($"{controller}_{method}");
                        var endpointNF = new EndpointMetrics
                        {
                            Controller = controller,
                            Method = method,
                            ControllerFile = controllerFile,
                            Complexity = "Very High",
                            NotFound = true,
                            OutputHtml = safeNameNF + ".html"
                        };
                        consolidated.Endpoints.Add(endpointNF);

                        if (!string.IsNullOrWhiteSpace(htmlDir))
                        {
                            var nfHtml = HtmlReportPrinter.BuildNotFoundPage($"{controller}.{method}", endpointNF.ControllerFile);
                            File.WriteAllText(Path.Combine(htmlDir, safeNameNF + ".html"), nfHtml, Encoding.UTF8);
                        }
                        continue;
                    }

                    Console.WriteLine($"\n=== Analyzing {controller}.{method} ===");

                    var graph = analyzer.BuildCallGraph(
                        root: rootMethod,
                        stopPredicate: m => stopClasses.Contains(m.ClassName),
                        ignoreCall: ignoreCall
                    );
                    printer.PrintSummary(graph);

                    // Collect metrics and queries
                    var nodes = GraphUniqueNodes(graph);
                    var analyzedNodes = nodes.Where(m => !stopClasses.Contains(m.ClassName)).ToList();

                    var perMethod = new List<MethodMetrics>();
                    var allNamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allNative = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allEntity = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allLoaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Domain loaders
                    var allClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var allProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var m in nodes)
                    {
                        var ents = HtmlReportPrinter.ExtractEntities(m, indexer.DomainEntities);
                        foreach (var e in ents) allEntities.Add(e);

                        var qs = QueryExtractor.Extract(m.FullText);
                        foreach (var s in qs.Named) allNamed.Add(s);
                        foreach (var s in qs.Native) allNative.Add(s);
                        foreach (var s in qs.Entity) allEntity.Add(s);
                        foreach (var s in qs.DomainLoaders) allLoaders.Add(s);

                        var project = ProjectResolver.GetProjectNameForFile(m.FilePath);
                        if (!string.IsNullOrEmpty(project)) allProjects.Add(project);

                        perMethod.Add(new MethodMetrics
                        {
                            Class = m.ClassName,
                            Method = m.Name,
                            File = m.FilePath,
                            Start = m.StartLine,
                            End = m.EndLine,
                            Loc = m.Metrics.LOC,
                            Cyclomatic = m.Metrics.Cyclomatic,
                            Complexity = ComplexityScale.MethodLabel(m.Metrics.Cyclomatic),
                            Entities = ents,
                            Project = project,
                            Queries = qs
                        });

                        allClasses.Add(m.ClassName);
                        allMethods.Add($"{m.ClassName}.{m.Name}");
                    }

                    // Resolve named queries (HBM) for endpoint aggregation
                    var namedResolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var name in allNamed)
                    {
                        var txt = namedIndex.TryGetText(name);
                        if (!string.IsNullOrWhiteSpace(txt))
                            namedResolved[name] = txt!;
                    }

                    int totalCyclomaticAnalyzed = analyzedNodes.Sum(x => x.Metrics.Cyclomatic);
                    int totalLocAnalyzed = analyzedNodes.Sum(x => x.Metrics.LOC);
                    int rootLoc = graph.Root.Metrics.LOC;
                    var funcLabel = ComplexityScale.FunctionLabel(totalCyclomaticAnalyzed);

                    var endpointMetrics = new EndpointMetrics
                    {
                        Controller = controller,
                        Method = method,
                        ControllerFile = controllerFile,
                        RootLoc = rootLoc,
                        TotalLoc = totalLocAnalyzed,
                        TotalCyclomatic = totalCyclomaticAnalyzed,
                        Complexity = funcLabel,
                        MethodsCount = analyzedNodes.Count,
                        ClassesCount = analyzedNodes.Select(m => m.ClassName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                        ImpactedClasses = allClasses.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                        ImpactedMethods = allMethods.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                        ImpactedEntities = allEntities.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                        ImpactedProjects = allProjects.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                        ImpactedQueries = new QuerySet
                        {
                            Named = allNamed.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                            Native = allNative.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                            Entity = allEntity.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                            DomainLoaders = allLoaders.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                            NamedResolved = namedResolved
                        },
                        Methods = perMethod,
                        NotFound = false
                    };

                    var safeName = SafeFileName($"{controller}_{method}");
                    endpointMetrics.OutputHtml = safeName + ".html";
                    consolidated.Endpoints.Add(endpointMetrics);

                    // Per-endpoint outputs
                    if (!string.IsNullOrWhiteSpace(jsonDir))
                    {
                        var outFile = Path.Combine(jsonDir, safeName + ".json");
                        File.WriteAllText(outFile, graph.ToJson());
                        Console.WriteLine($"Wrote JSON: {outFile}");
                    }
                    if (!string.IsNullOrWhiteSpace(mdDir))
                    {
                        var outFile = Path.Combine(mdDir, safeName + ".md");
                        File.WriteAllText(outFile, printer.ToMarkdown(graph));
                        Console.WriteLine($"Wrote Markdown: {outFile}");
                    }
                    if (!string.IsNullOrWhiteSpace(htmlDir))
                    {
                        var html = HtmlReportPrinter.BuildThreeLayerHtml(
                            g: graph,
                            indexer: indexer,
                            stopClasses: stopClasses,
                            namedIndex: namedIndex,
                            title: $"{controller}.{method} – Dependency & Complexity"
                        );
                        var outFile = Path.Combine(htmlDir, safeName + ".html");
                        File.WriteAllText(outFile, html, Encoding.UTF8);
                        Console.WriteLine($"Wrote HTML: {outFile}");
                    }
                }

                // Rollup and write consolidated JSON
                consolidated.Rollup = ConsolidatedRollup.From(consolidated);

                if (!string.IsNullOrWhiteSpace(jsonDir))
                {
                    var allJsonPath = Path.Combine(jsonDir, "all_metrics.json");
                    File.WriteAllText(allJsonPath, JsonSerializer.Serialize(consolidated, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }), Encoding.UTF8);
                    Console.WriteLine($"Wrote consolidated JSON: {allJsonPath}");
                }

                // Dashboard HTML (embedded JSON)
                if (!string.IsNullOrWhiteSpace(htmlDir))
                {
                    var dashboardHtml = DashboardBuilder.Build(consolidated, title: "Endpoint Complexity Dashboard");
                    var outFile = Path.Combine(htmlDir, "dashboard.html");
                    File.WriteAllText(outFile, dashboardHtml, Encoding.UTF8);
                    Console.WriteLine($"Wrote dashboard: {outFile}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex);
                return 1;
            }
        }

        // ---------- Helpers for Program ----------

        private static IEnumerable<(string Controller, string Method)> EnumerateTargetsFromUrls(IEnumerable<string> lines)
        {
            foreach (var rawLine in lines ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                var line = rawLine.Trim();
                if (line.Equals("N/A", StringComparison.OrdinalIgnoreCase)) continue;

                var fragments = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var fragmentRaw in fragments)
                {
                    var fragment = fragmentRaw.Trim();
                    var parts = fragment.Split('?', 2);
                    if (parts.Length < 2) continue;

                    var controller = parts[0].Trim().Replace(".aspx", "", StringComparison.OrdinalIgnoreCase);
                    var query = parts[1];
                    var qs = query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                    var methodParam = qs.FirstOrDefault(q => q.TrimStart().StartsWith("method=", StringComparison.OrdinalIgnoreCase));
                    if (methodParam == null) continue;

                    var kv = methodParam.Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;

                    var method = kv[1].Trim();
                    if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(method)) continue;

                    yield return (controller, method);
                }
            }
        }

        private static string? EnsureOutDirectory(string? pathOrFile)
        {
            if (string.IsNullOrWhiteSpace(pathOrFile)) return null;
            string dir = Path.HasExtension(pathOrFile)
                ? (Path.GetDirectoryName(pathOrFile) ?? Directory.GetCurrentDirectory())
                : pathOrFile;
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string SafeFileName(string name)
            => Regex.Replace(name, @"[^A-Za-z0-9_.-]+", "_");

        private static List<MethodDef> GraphUniqueNodes(CallGraph g)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<MethodDef>();
            void Add(MethodDef m)
            {
                var key = m.FilePath + "|" + m.StartLine;
                if (set.Add(key)) list.Add(m);
            }
            Add(g.Root);
            foreach (var e in g.Edges) { Add(e.From); Add(e.To); }
            return list;
        }

        private static bool IgnoreRules(Analyzer.CallCandidate c)
        {
            var name = c.Name ?? "";
            var raw = (c.Raw ?? "").ToLowerInvariant();
            var qual = (c.Qualifier ?? "").ToLowerInvariant();

            // Utility / built-ins
            if (name.Equals("Equals", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("IsNullOrEmpty", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("ToString", StringComparison.OrdinalIgnoreCase)) return true;

            // Additional helpers to ignore
            if (name.Equals("IfCondition", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("IfNull", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("GetCollection", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("UpdateDependentTables", StringComparison.OrdinalIgnoreCase)) return true;

            // AmsEntityManager.Remove
            if (name.Equals("Remove", StringComparison.OrdinalIgnoreCase) &&
                (qual.EndsWith("amsentitymanager") || raw.Contains("amsentitymanager.remove(")))
                return true;

            // imageHeader.StartsWith / StartWith
            if ((name.Equals("StartsWith", StringComparison.OrdinalIgnoreCase) ||
                 name.Equals("StartWith", StringComparison.OrdinalIgnoreCase)) &&
                 Regex.IsMatch(raw, @"\bimageheader\s*\.\s*starts?with\("))
                return true;

            // .Add on ReportColumns / ReportingDataSheet / DataSheet / DocumentTableRow
            if (name.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                if (Regex.IsMatch(raw, @"\b(reportcolumns|reportingdatasheet|datasheet|documenttablerow)\s*\.\s*add\s*\(", RegexOptions.IgnoreCase))
                    return true;

                // extra safety: any qualifier containing these tokens
                if (!string.IsNullOrEmpty(qual))
                {
                    if (qual.Contains("report") || qual.Contains("datasheet") || qual.Contains("documenttablerow"))
                        return true;
                }
            }

            return false;
        }
    }

    // ================== Options ==================

    public sealed class Options
    {
        public string Root { get; private set; } = Directory.GetCurrentDirectory();
        public string? StartFile { get; private set; } // compat (unused here)
        public string MethodName { get; private set; } = "UnassignAssetPropertyManagementSystem"; // compat
        public string FileGlob { get; private set; } = "*.cs";
        public int MaxDepth { get; private set; } = 3;
        public string? OutJson { get; private set; }
        public string? OutMarkdown { get; private set; }
        public string? OutHtml { get; private set; }
        public string? MappingDir { get; private set; } // NEW
        public bool ShowHelp { get; private set; }

        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--root": o.Root = args[++i]; break;
                    case "--start-file": o.StartFile = args[++i]; break; // compat
                    case "--method": o.MethodName = args[++i]; break;     // compat
                    case "--glob": o.FileGlob = args[++i]; break;
                    case "--depth": o.MaxDepth = int.Parse(args[++i]); break;
                    case "--out-json": o.OutJson = args[++i]; break;
                    case "--out-md": o.OutMarkdown = args[++i]; break;
                    case "--out-html": o.OutHtml = args[++i]; break;
                    case "--mapping-dir": o.MappingDir = args[++i]; break; // NEW
                    case "-h":
                    case "--help": o.ShowHelp = true; break;
                    default:
                        Console.Error.WriteLine($"Unknown arg: {a}");
                        o.ShowHelp = true;
                        break;
                }
            }
            return o;
        }

        public static void PrintHelp()
        {
            Console.WriteLine(@"
UnassignAnalyzer

USAGE
  dotnet run -- --root <src_root> [--depth 3]
    [--out-json out.json] [--out-md out.md] [--out-html out.html]
    [--mapping-dir D:\mapping]

NOTES
  - mapping-dir is optional; if omitted, *.hbm.xml files are scanned under --root too.
");
        }
    }

    // ================== Model ==================

    public sealed class MethodDef
    {
        public string Name { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string Signature { get; set; } = "";
        public string FilePath { get; set; } = "";
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Body { get; set; } = "";
        public string FullText { get; set; } = "";
        public ComplexityMetrics Metrics => ComplexityMetrics.FromCode(FullText);
        public string Location => $"{FilePath}({StartLine}–{EndLine})";
    }

    public sealed class ConsolidatedMetrics
    {
        public List<EndpointMetrics> Endpoints { get; set; } = new();
        public ConsolidatedRollup? Rollup { get; set; }
    }

    public sealed class EndpointMetrics
    {
        public string Controller { get; set; } = "";
        public string Method { get; set; } = "";
        public string ControllerFile { get; set; } = "";
        public int RootLoc { get; set; }
        public int TotalLoc { get; set; }
        public int TotalCyclomatic { get; set; }
        public string Complexity { get; set; } = "";
        public int MethodsCount { get; set; }
        public int ClassesCount { get; set; }
        public List<string> ImpactedClasses { get; set; } = new();
        public List<string> ImpactedMethods { get; set; } = new();
        public List<string> ImpactedEntities { get; set; } = new();
        public List<string> ImpactedProjects { get; set; } = new();
        public QuerySet ImpactedQueries { get; set; } = new();
        public List<MethodMetrics> Methods { get; set; } = new();
        public bool NotFound { get; set; }
        public string? OutputHtml { get; set; }
    }

    public sealed class MethodMetrics
    {
        public string Class { get; set; } = "";
        public string Method { get; set; } = "";
        public string File { get; set; } = "";
        public int Start { get; set; }
        public int End { get; set; }
        public int Loc { get; set; }
        public int Cyclomatic { get; set; }
        public string Complexity { get; set; } = "";
        public List<string> Entities { get; set; } = new();
        public string? Project { get; set; }
        public QuerySet Queries { get; set; } = new();
    }

    public sealed class QuerySet
    {
        public List<string> Named { get; set; } = new();                  // just the HBM names
        public List<string> NamedDisplay { get; set; } = new();           // pretty call signatures with types
        public List<string> Entity { get; set; } = new();
        public List<string> Native { get; set; } = new();
        public List<string> DomainLoaders { get; set; } = new();          // e.g., Asset.FindById, Asset.FindAll
        public Dictionary<string, string> NamedResolved { get; set; } = new();
    }

    public sealed class ConsolidatedRollup
    {
        public int TotalEndpoints { get; set; }
        public Dictionary<string, int> ByComplexity { get; set; } = new();  // Simple/Medium/High/Very High
        public List<string> Projects { get; set; } = new();
        public List<string> Classes { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public QuerySet Queries { get; set; } = new();

        public static ConsolidatedRollup From(ConsolidatedMetrics cm)
        {
            var r = new ConsolidatedRollup();
            r.TotalEndpoints = cm.Endpoints.Count;
            r.ByComplexity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Simple"] = cm.Endpoints.Count(e => e.Complexity.Equals("Simple", StringComparison.OrdinalIgnoreCase)),
                ["Medium"] = cm.Endpoints.Count(e => e.Complexity.Equals("Medium", StringComparison.OrdinalIgnoreCase)),
                ["High"] = cm.Endpoints.Count(e => e.Complexity.Equals("High", StringComparison.OrdinalIgnoreCase)),
                ["Very High"] = cm.Endpoints.Count(e => e.Complexity.Equals("Very High", StringComparison.OrdinalIgnoreCase))
            };

            var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var native = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entityQ = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in cm.Endpoints)
            {
                foreach (var p in e.ImpactedProjects) projects.Add(p);
                foreach (var c in e.ImpactedClasses) classes.Add(c);
                foreach (var en in e.ImpactedEntities) entities.Add(en);
                foreach (var s in e.ImpactedQueries.Named) named.Add(s);
                foreach (var s in e.ImpactedQueries.Native) native.Add(s);
                foreach (var s in e.ImpactedQueries.Entity) entityQ.Add(s);
                foreach (var s in e.ImpactedQueries.DomainLoaders) loaders.Add(s);
            }

            r.Projects = projects.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            r.Classes = classes.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            r.Entities = entities.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            r.Queries = new QuerySet
            {
                Named = named.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                Native = native.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                Entity = entityQ.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                DomainLoaders = loaders.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList()
            };
            return r;
        }
    }

    // ================== Indexer ==================

    public sealed class SourceIndexer
    {
        private readonly string _root;
        private readonly string _glob;
        private readonly List<MethodDef> _methods = new();
        private readonly Dictionary<string, List<MethodDef>> _methodsByName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<MethodDef>> _methodsByClass = new(StringComparer.Ordinal);

        private readonly HashSet<string> _allClassNames = new(StringComparer.Ordinal);
        private readonly HashSet<string> _domainEntities = new(StringComparer.OrdinalIgnoreCase);

        public SourceIndexer(string root, string glob)
        {
            _root = Path.GetFullPath(root);
            _glob = glob;
        }

        public void Build()
        {
            foreach (var file in Directory.EnumerateFiles(_root, _glob, SearchOption.AllDirectories))
            {
                if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                IndexFile(file);
            }
        }

        public IReadOnlyCollection<string> AllClassNames => _allClassNames;
        public IReadOnlyCollection<string> DomainEntities => _domainEntities;

        private static readonly Regex ClassRx = new(@"\b(class)\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
        private static readonly Regex NamespaceRx = new(@"\bnamespace\s+(?<ns>[\w\.]+)", RegexOptions.Compiled);
        private static readonly Regex MethodHeaderRx = new(
            @"(?<header>\b(public|private|protected|internal)\s+[^\n\{;]*\b(?<name>[A-Za-z_]\w*)\s*\((?<params>[^\)]*)\)\s*\{)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private void IndexFile(string file)
        {
            var text = File.ReadAllText(file);
            var classes = new List<(string name, int index)>();
            foreach (Match m in ClassRx.Matches(text))
                classes.Add((m.Groups["name"].Value, m.Index));

            string ns = "";
            var nsMatch = NamespaceRx.Match(text);
            if (nsMatch.Success) ns = nsMatch.Groups["ns"].Value;

            // Track classes + mark domain entities
            foreach (var (cname, _) in classes)
            {
                _allClassNames.Add(cname);
                if (file.IndexOf("Domain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ns.IndexOf(".Domain.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _domainEntities.Add(cname);
                }
            }

            int pos = 0;
            while (true)
            {
                var m = MethodHeaderRx.Match(text, pos);
                if (!m.Success) break;

                int bodyStart = text.IndexOf("{", m.Groups["header"].Index, StringComparison.Ordinal);
                if (bodyStart < 0) break;
                int bodyEnd = BraceMatch(text, bodyStart);
                if (bodyEnd < 0) break;

                var methodName = m.Groups["name"].Value;
                var signature = text.Substring(m.Groups["header"].Index, bodyStart - m.Groups["header"].Index).Trim();
                var full = text.Substring(m.Groups["header"].Index, bodyEnd - m.Groups["header"].Index + 1);

                string className = "";
                foreach (var (cname, idx) in classes)
                {
                    if (idx < m.Groups["header"].Index) className = cname; else break;
                }

                var (startLine, endLine) = ComputeLines(text, m.Groups["header"].Index, bodyEnd);

                var def = new MethodDef
                {
                    Name = methodName,
                    ClassName = className,
                    Namespace = ns,
                    Signature = signature,
                    FilePath = file,
                    StartLine = startLine,
                    EndLine = endLine,
                    Body = text.Substring(bodyStart + 1, bodyEnd - bodyStart - 1),
                    FullText = full
                };

                _methods.Add(def);

                if (!_methodsByName.TryGetValue(def.Name, out var listByName))
                {
                    listByName = new List<MethodDef>();
                    _methodsByName[def.Name] = listByName;
                }
                listByName.Add(def);

                if (!_methodsByClass.TryGetValue(def.ClassName, out var listByClass))
                {
                    listByClass = new List<MethodDef>();
                    _methodsByClass[def.ClassName] = listByClass;
                }
                listByClass.Add(def);

                pos = bodyEnd + 1;
            }
        }

        public MethodDef? FindMethodInFile(string filePath, string methodName)
        {
            filePath = Path.GetFullPath(filePath);
            return _methods.Where(m => m.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) && m.Name == methodName)
                           .OrderBy(m => m.StartLine)
                           .FirstOrDefault();
        }

        public List<MethodDef> FindMethodsByName(string methodName)
            => _methodsByName.TryGetValue(methodName, out var list) ? list.ToList() : new List<MethodDef>();

        public List<MethodDef> FindMethodsByClass(string className)
            => _methodsByClass.TryGetValue(className, out var list) ? list.ToList() : new List<MethodDef>();

        private static (int start, int end) ComputeLines(string text, int startIdx, int endIdx)
        {
            var before = text.Substring(0, startIdx);
            int startLine = before.Count(c => c == '\n') + 1;
            int endLine = startLine + text.Substring(startIdx, endIdx - startIdx + 1).Count(c => c == '\n');
            return (startLine, endLine);
        }

        private static int BraceMatch(string text, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }

    // ================== Analyzer ==================

    public sealed class Analyzer
    {
        private readonly SourceIndexer _indexer;
        private readonly int _maxDepth;
        private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "if","for","foreach","while","switch","lock","using","return","new","nameof","typeof","throw","await","yield","catch"
        };

        public Analyzer(SourceIndexer indexer, int maxDepth)
        {
            _indexer = indexer;
            _maxDepth = Math.Max(1, maxDepth);
        }

        public CallGraph BuildCallGraph(
            MethodDef root,
            Func<MethodDef, bool>? stopPredicate = null,
            Func<CallCandidate, bool>? ignoreCall = null)
        {
            var graph = new CallGraph(root);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            DFS(root, 0, graph, visited, stopPredicate, ignoreCall);
            return graph;
        }

        private void DFS(
            MethodDef method, int depth,
            CallGraph graph, HashSet<string> visited,
            Func<MethodDef, bool>? stopPredicate,
            Func<CallCandidate, bool>? ignoreCall)
        {
            string key = method.FilePath + "|" + method.StartLine;
            if (!visited.Add(key)) return;
            if (depth >= _maxDepth) return;
            if (stopPredicate != null && stopPredicate(method)) return;

            foreach (var call in ExtractCalls(method))
            {
                if (ignoreCall != null && ignoreCall(call)) continue;

                var resolved = ResolveCall(call);
                if (resolved.Count == 0) continue;

                foreach (var target in resolved)
                {
                    graph.AddEdge(method, target, call.Raw);
                    if (stopPredicate == null || !stopPredicate(target))
                    {
                        DFS(target, depth + 1, graph, visited, stopPredicate, ignoreCall);
                    }
                }
            }
        }

        public sealed class CallCandidate
        {
            public string? Qualifier { get; set; }  // e.g., "AmsEntityManager"
            public string Name { get; set; } = "";
            public string Raw { get; set; } = "";
        }

        private static readonly Regex CallRx =
            new(@"(?<raw>(?<qual>[A-Za-z_][\w\.<>]*)\s*\.\s*)?(?<name>[A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);

        private IEnumerable<CallCandidate> ExtractCalls(MethodDef method)
        {
            string code = StripStringsAndComments(method.FullText);
            foreach (Match m in CallRx.Matches(code))
            {
                var name = m.Groups["name"].Value;
                var q = m.Groups["qual"].Success ? m.Groups["qual"].Value : null;

                if (SkipNames.Contains(name)) continue;
                if (q != null && (q == "this" || q == "base")) q = null;

                yield return new CallCandidate { Qualifier = q, Name = name, Raw = m.Groups["raw"].Value + name + "(" };
            }
        }

        private List<MethodDef> ResolveCall(CallCandidate call)
        {
            var results = new List<MethodDef>();

            if (!string.IsNullOrWhiteSpace(call.Qualifier))
            {
                string className = call.Qualifier.Split('.').Last();
                var inClass = _indexer.FindMethodsByClass(className).Where(m => m.Name == call.Name).ToList();
                if (inClass.Count > 0) results.AddRange(inClass);

                var ext = _indexer.FindMethodsByName(call.Name)
                                  .Where(m => m.Signature.Contains("this " + className + " ", StringComparison.Ordinal))
                                  .ToList();
                if (ext.Count > 0) results.AddRange(ext);
            }

            if (results.Count == 0)
                results.AddRange(_indexer.FindMethodsByName(call.Name));

            return results
                .GroupBy(m => m.FilePath + "|" + m.StartLine)
                .Select(g => g.First())
                .ToList();
        }

        private static string StripStringsAndComments(string s)
        {
            s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
            s = Regex.Replace(s, @"//.*", "");
            s = Regex.Replace(s, @"@""(?:[^""]|"""")*""", "\"\"");
            s = Regex.Replace(s, @"""(?:\\.|[^""\\])*""", "\"\"");
            return s;
        }
    }

    // ================== Metrics & Scale ==================

    public sealed class ComplexityMetrics
    {
        public int LOC { get; private set; }
        public int Cyclomatic { get; private set; }
        public int If { get; private set; }
        public int For { get; private set; }
        public int ForEach { get; private set; }
        public int While { get; private set; }
        public int Case { get; private set; }
        public int Catch { get; private set; }
        public int Ternary { get; private set; }
        public int AndOr { get; private set; }

        public static ComplexityMetrics FromCode(string code)
        {
            string s = Strip(code);
            int Count(string rx) => Regex.Matches(s, rx).Count;

            var m = new ComplexityMetrics();
            m.LOC = code.Split('\n').Length;
            m.If = Count(@"\bif\s*\(");
            m.For = Count(@"\bfor\s*\(");
            m.ForEach = Count(@"\bforeach\s*\(");
            m.While = Count(@"\bwhile\s*\(");
            m.Case = Count(@"\bcase\b");
            m.Catch = Count(@"\bcatch\b");
            m.Ternary = Count(@"\?[^:;\n]+\:");
            m.AndOr = Count(@"&&|\|\|");
            m.Cyclomatic = 1 + m.If + m.For + m.ForEach + m.While + m.Case + m.Catch + m.Ternary + m.AndOr;
            return m;
        }

        private static string Strip(string s)
        {
            s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
            s = Regex.Replace(s, @"//.*", "");
            s = Regex.Replace(s, @"@""(?:[^""]|"""")*""", "\"\"");
            s = Regex.Replace(s, @"""(?:\\.|[^""\\])*""", "\"\"");
            return s;
        }
    }

    public static class ComplexityScale
    {
        public static string MethodLabel(int cyclomatic) =>
            cyclomatic <= 5 ? "Simple" :
            cyclomatic <= 10 ? "Medium" :
            cyclomatic <= 20 ? "High" : "Very High";

        public static string FunctionLabel(int totalCyclomatic) =>
            totalCyclomatic <= 10 ? "Simple" :
            totalCyclomatic <= 30 ? "Medium" :
            totalCyclomatic <= 60 ? "High" : "Very High";

        public static string CssClass(string label)
            => label.Replace(" ", "").ToLowerInvariant(); // simple, medium, high, veryhigh
    }

    // ================== Query Extractor ==================

    public static class QueryExtractor
    {
        // capture first string literal after the call (supports @"" and "" with "" escapes)
        private static readonly Regex FirstStringArgRx = new(@"\(\s*(?:@?""(?<s>(?:[^""]|"""")*)"")", RegexOptions.Compiled);

        // Allow optional generics between method name and '('  e.g. FindByNamedQuery<Asset>("Name", ...)
        private static string CallPattern(string callName)
            => @"\b" + Regex.Escape(callName) + @"\s*(?:<(?<gen>[^>]+)>)?\s*\(";

        private static string CleanType(string gen)
        {
            if (string.IsNullOrWhiteSpace(gen)) return gen;
            var t = gen.Trim();
            // Remove namespaces and constraints
            if (t.Contains(".")) t = t.Split('.').Last();
            // strip generic commas if present (e.g., Foo<Bar,Baz> -> Foo)
            t = Regex.Replace(t, @"[<,].*$", "");
            return t.Trim();
        }

        public static QuerySet Extract(string methodFullText)
        {
            var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var namedDisp = new List<string>();
            var native = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entity = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(HashSet<string> set, string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                var norm = s.Replace("\"\"", "\"").Trim();
                if (norm.Length > 300) norm = norm.Substring(0, 300) + " …";
                set.Add(norm);
            }

            string s = StripComments(methodFullText);

            // 1) Named queries (generic-aware)
            var namedRx = new Regex(CallPattern("GetNamedQuery") + "|" + CallPattern("FindAllByNamedQuery") + "|" + CallPattern("FindByNamedQuery") + "|" + CallPattern("NamedQuery"), RegexOptions.IgnoreCase);
            foreach (Match m in namedRx.Matches(s))
            {
                string gen = m.Groups["gen"]?.Value ?? "";
                var tail = s.Substring(m.Index);
                var ms = FirstStringArgRx.Match(tail);
                if (ms.Success)
                {
                    var qname = ms.Groups["s"].Value;
                    Add(named, qname);

                    var genClean = CleanType(gen);
                    var callName = s.Substring(m.Index, m.Length);
                    // Rebuild a nice display: Method<Gen>("QueryName")
                    if (!string.IsNullOrWhiteSpace(genClean))
                        namedDisp.Add($"{ExtractMethodName(callName)}<{genClean}>(\"{qname}\")");
                    else
                        namedDisp.Add($"{ExtractMethodName(callName)}(\"{qname}\")");
                }
            }

            // 2) Entity/HQL
            foreach (var call in new[] { "CreateQuery" })
            {
                foreach (Match m in Regex.Matches(s, CallPattern(call), RegexOptions.IgnoreCase))
                {
                    var tail = s.Substring(m.Index);
                    var ms = FirstStringArgRx.Match(tail);
                    if (ms.Success) Add(entity, ms.Groups["s"].Value);
                }
            }
            // LINQ style
            if (Regex.IsMatch(s, @"\bQuery<\s*[A-Za-z_]\w*", RegexOptions.IgnoreCase)) entity.Add("Session.Query<T>(...)");
            if (Regex.IsMatch(s, @"\bQueryOver<\s*[A-Za-z_]\w*", RegexOptions.IgnoreCase)) entity.Add("QueryOver<T>(...)");

            // Direct entity loaders (typed) -> put ALSO into DomainLoaders as DomainName.FindById / FindAll
            var findRx = new Regex(@"\b(?:AmsEntityManager|EntityManager|SecureEntityManager)\s*\.\s*Find\s*<\s*(?<type>[A-Za-z_][\w\.]*)\s*>\s*\(", RegexOptions.IgnoreCase);
            var findAllRx = new Regex(@"\b(?:AmsEntityManager|EntityManager|SecureEntityManager)\s*\.\s*FindAll\s*<\s*(?<type>[A-Za-z_][\w\.]*)\s*>\s*\(", RegexOptions.IgnoreCase);

            foreach (Match m in findRx.Matches(s))
            {
                var t = CleanType(m.Groups["type"].Value);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    entity.Add($"EntityManager.Find<{t}>(...)");
                    loaders.Add($"{t}.FindById");
                }
            }
            foreach (Match m in findAllRx.Matches(s))
            {
                var t = CleanType(m.Groups["type"].Value);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    entity.Add($"EntityManager.FindAll<{t}>(...)");
                    loaders.Add($"{t}.FindAll");
                }
            }

            // 3) Native SQL
            foreach (var call in new[] { "CreateSQLQuery", "ExecuteSQLQuery" })
            {
                foreach (Match m in Regex.Matches(s, CallPattern(call), RegexOptions.IgnoreCase))
                {
                    var tail = s.Substring(m.Index);
                    var ms = FirstStringArgRx.Match(tail);
                    if (ms.Success) Add(native, ms.Groups["s"].Value);
                }
            }
            if (Regex.IsMatch(s, @"new\s+SqlCommand\s*\(", RegexOptions.IgnoreCase)) native.Add("SqlCommand(...)");

            return new QuerySet
            {
                Named = named.ToList(),
                NamedDisplay = namedDisp,
                Native = native.ToList(),
                Entity = entity.ToList(),
                DomainLoaders = loaders.ToList(),
                NamedResolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string ExtractMethodName(string callSlice)
        {
            var m = Regex.Match(callSlice, @"\b([A-Za-z_]\w*)\s*(?:<|[\s\(])");
            return m.Success ? m.Groups[1].Value : "NamedQuery";
        }

        private static string StripComments(string s)
        {
            s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
            s = Regex.Replace(s, @"//.*", "");
            return s;
        }
    }

    // ================== Project Resolver ==================

    public static class ProjectResolver
    {
        private static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static string? GetProjectNameForFile(string file)
        {
            try
            {
                var dir = Path.GetDirectoryName(file);
                if (dir == null) return null;

                string cur = dir;
                while (!string.IsNullOrEmpty(cur))
                {
                    if (Cache.TryGetValue(cur, out var pn)) return pn;

                    var csproj = Directory.EnumerateFiles(cur, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (csproj != null)
                    {
                        var name = Path.GetFileNameWithoutExtension(csproj);
                        Cache[cur] = name;
                        return name;
                    }
                    var parent = Directory.GetParent(cur);
                    if (parent == null) break;
                    cur = parent.FullName;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }

    // ================== Named Query Resolver (NHibernate .hbm.xml) ==================

    public sealed class NamedQueryDef
    {
        public string Name { get; init; } = "";
        public string Kind { get; init; } = ""; // "hql" or "sql"
        public string Text { get; init; } = "";
        public string File { get; init; } = "";
    }

    public sealed class NamedQueryIndex
    {
        private readonly Dictionary<string, NamedQueryDef> _map;

        public NamedQueryIndex(Dictionary<string, NamedQueryDef> map)
        {
            _map = map;
        }

        public bool TryGet(string name, out NamedQueryDef def) => _map.TryGetValue(name, out def!);

        public string? TryGetText(string name)
            => _map.TryGetValue(name, out var d) ? d.Text : null;
    }

    public static class NamedQueryResolver
    {
        public static NamedQueryIndex Build(string root, string? mappingDir)
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(mappingDir) && Directory.Exists(mappingDir))
            {
                foreach (var f in Directory.EnumerateFiles(mappingDir, "*.hbm.xml", SearchOption.AllDirectories))
                    files.Add(f);
            }

            foreach (var f in Directory.EnumerateFiles(root, "*.hbm.xml", SearchOption.AllDirectories))
                files.Add(f);

            var map = new Dictionary<string, NamedQueryDef>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    foreach (var el in doc.Descendants())
                    {
                        var local = el.Name.LocalName;
                        if (local == "query" || local == "sql-query")
                        {
                            var nameAttr = el.Attribute("name");
                            if (nameAttr == null) continue;
                            var name = nameAttr.Value?.Trim();
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            var text = (el.Value ?? "").Trim();
                            text = Normalize(text);

                            var kind = local == "sql-query" ? "sql" : "hql";

                            if (!map.ContainsKey(name))
                            {
                                map[name] = new NamedQueryDef
                                {
                                    Name = name,
                                    Kind = kind,
                                    Text = text,
                                    File = file
                                };
                            }
                        }
                    }
                }
                catch { /* ignore invalid mapping files */ }
            }

            return new NamedQueryIndex(map);
        }

        private static string Normalize(string s)
        {
            var lines = s.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = Regex.Replace(lines[i], @"\s+", " ").Trim();
            var joined = string.Join(Environment.NewLine, lines.Where(l => l.Length > 0));
            if (joined.Length > 1000) joined = joined.Substring(0, 1000) + " …";
            return joined;
        }
    }

    // ================== Graph ==================

    public sealed class CallGraph
    {
        public MethodDef Root { get; }
        public List<Edge> Edges { get; } = new();
        public HashSet<string> Nodes { get; } = new();

        public CallGraph(MethodDef root)
        {
            Root = root;
            Nodes.Add(Key(root));
        }

        public void AddEdge(MethodDef from, MethodDef to, string via)
        {
            Nodes.Add(Key(from));
            Nodes.Add(Key(to));
            Edges.Add(new Edge(from, to, via));
        }

        public string ToJson()
        {
            var nodes = Edges
                .SelectMany(e => new[] { e.From, e.To })
                .Append(Root)
                .DistinctBy(m => m.FilePath + "|" + m.StartLine)
                .Select(NodeObj)
                .ToList();

            var obj = new
            {
                root = NodeObj(Root),
                nodes = nodes,
                edges = Edges.Select(e => new
                {
                    from = NodeKey(e.From),
                    to = NodeKey(e.To),
                    via = e.Via
                })
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string Key(MethodDef m) => m.FilePath + "|" + m.StartLine;
        private static string NodeKey(MethodDef m) => Key(m);
        private static object NodeObj(MethodDef m) => new
        {
            name = m.Name,
            @class = m.ClassName,
            ns = m.Namespace,
            file = m.FilePath,
            start = m.StartLine,
            end = m.EndLine,
            metrics = new
            {
                loc = m.Metrics.LOC,
                cyclomatic = m.Metrics.Cyclomatic,
                @if = m.Metrics.If,
                _for = m.Metrics.For,
                foreach_ = m.Metrics.ForEach,
                @while = m.Metrics.While,
                @case = m.Metrics.Case,
                @catch = m.Metrics.Catch,
                ternary = m.Metrics.Ternary,
                and_or = m.Metrics.AndOr
            }
        };

        public sealed record Edge(MethodDef From, MethodDef To, string Via);
    }

    // ================== Markdown Printer ==================

    public sealed class ReportPrinter
    {
        public void PrintSummary(CallGraph g)
        {
            Console.WriteLine("== Call chain (heuristic) ==\n");
            Console.WriteLine($"{Path.GetFileName(g.Root.FilePath)}:{g.Root.StartLine}  {g.Root.ClassName}.{g.Root.Name}()  [Cyclomatic:{g.Root.Metrics.Cyclomatic}, LOC:{g.Root.Metrics.LOC}]");
            foreach (var e in g.Edges)
            {
                Console.WriteLine($"  ├─{Path.GetFileName(e.To.FilePath)}:{e.To.StartLine}  {e.To.ClassName}.{e.To.Name}()  via `{e.Via}`  [C:{e.To.Metrics.Cyclomatic}, LOC:{e.To.Metrics.LOC}]");
            }
        }

        public string ToMarkdown(CallGraph g)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Call Graph & Complexity");
            sb.AppendLine();
            sb.AppendLine($"**Root:** `{g.Root.ClassName}.{g.Root.Name}`  \n**File:** `{g.Root.FilePath}`  \n**Lines:** {g.Root.StartLine}–{g.Root.EndLine}  ");
            sb.AppendLine();
            sb.AppendLine("## Methods");

            var set = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<MethodDef>();
            void Add(MethodDef m)
            {
                var key = m.FilePath + "|" + m.StartLine;
                if (set.Add(key)) list.Add(m);
            }
            Add(g.Root);
            foreach (var e in g.Edges) { Add(e.From); Add(e.To); }

            foreach (var m in list)
            {
                sb.AppendLine($"### {m.ClassName}.{m.Name}  \n**File:** `{m.FilePath}`  \n**Lines:** {m.StartLine}–{m.EndLine}  ");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                var lines = File.ReadAllLines(m.FilePath);
                for (int i = m.StartLine - 1; i < m.EndLine && i < lines.Length; i++)
                {
                    sb.AppendLine($"{i + 1,5}: {lines[i]}");
                }
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine($"Metrics: Cyclomatic **{m.Metrics.Cyclomatic}** ({ComplexityScale.MethodLabel(m.Metrics.Cyclomatic)}), LOC **{m.Metrics.LOC}**");

                // Quick query list (compact)
                var q = QueryExtractor.Extract(m.FullText);
                if (q.Named.Count + q.Entity.Count + q.Native.Count + q.DomainLoaders.Count > 0)
                {
                    sb.AppendLine("- **Named queries:** " + (q.NamedDisplay.Count == 0 ? "—" : string.Join("; ", q.NamedDisplay)));
                    sb.AppendLine("- **Loaders (Domain-style):** " + (q.DomainLoaders.Count == 0 ? "—" : string.Join("; ", q.DomainLoaders)));
                    sb.AppendLine("- **Entity queries:** " + (q.Entity.Count == 0 ? "—" : string.Join("; ", q.Entity)));
                    sb.AppendLine("- **Native SQL:** " + (q.Native.Count == 0 ? "—" : string.Join("; ", q.Native)));
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Edges");
            foreach (var e in g.Edges)
            {
                sb.AppendLine($"- `{Short(e.From)}` → `{Short(e.To)}`  (via **{e.Via}**)");
            }
            return sb.ToString();
        }

        private static string Short(MethodDef m) => $"{Path.GetFileName(m.FilePath)}:{m.StartLine} {m.ClassName}.{m.Name}";
    }

    // ================== HTML Printer (per endpoint) ==================

    public static class HtmlReportPrinter
    {
        private static readonly HashSet<string> EntityExclusions = new(StringComparer.OrdinalIgnoreCase)
        {
            "ReportColumns","ReportingDataSheet","DataSheet","DocumentTableRow","ImageHeader"
        };

        public static string BuildNotFoundPage(string endpoint, string attemptedFile)
        {
            return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>{Html(endpoint)} – Not Found</title>
<style>body{{font-family:Segoe UI,Roboto,Arial,sans-serif;margin:24px}} .card{{border:1px solid #ddd;border-radius:8px;padding:14px}}</style>
</head><body>
<h1>{Html(endpoint)}</h1>
<div class='card'>The requested controller method could not be located in source. This endpoint is counted as <b>Very High</b> complexity for tracking.</div>
<div class='card' style='margin-top:12px'><b>Attempted file:</b> {Html(attemptedFile)}</div>
</body></html>";
        }

        public static string BuildThreeLayerHtml(
            CallGraph g,
            SourceIndexer indexer,
            HashSet<string> stopClasses,
            NamedQueryIndex namedIndex,
            string title)
        {
            var nodes = UniqueNodes(g);
            var analyzed = nodes.Where(m => !stopClasses.Contains(m.ClassName)).ToList();
            var terminals = nodes.Where(m => stopClasses.Contains(m.ClassName)).ToList();

            // Entities/Queries per method (and resolve named)
            var entitiesByMethod = new Dictionary<MethodDef, List<String>>();
            var queriesByMethod = new Dictionary<MethodDef, QuerySet>();
            foreach (var m in nodes)
            {
                entitiesByMethod[m] = ExtractEntities(m, indexer.DomainEntities);
                var qs = QueryExtractor.Extract(m.FullText);
                foreach (var n in qs.Named.ToList())
                {
                    var txt = namedIndex.TryGetText(n);
                    if (!string.IsNullOrWhiteSpace(txt) && !qs.NamedResolved.ContainsKey(n))
                        qs.NamedResolved[n] = txt!;
                }
                queriesByMethod[m] = qs;
            }

            // Aggregates
            int totalCyclomaticAnalyzed = analyzed.Sum(m => m.Metrics.Cyclomatic);
            int totalLOCAnalyzed = analyzed.Sum(m => m.Metrics.LOC);
            int rootLOC = g.Root.Metrics.LOC;
            var funcLabel = ComplexityScale.FunctionLabel(totalCyclomaticAnalyzed);

            var impactedClasses = analyzed.Select(m => m.ClassName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var impactedMethods = analyzed.Select(m => m.ClassName + "." + m.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var impactedEntities = analyzed.SelectMany(m => entitiesByMethod[m]).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

            var allNamed = nodes.SelectMany(m => queriesByMethod[m].NamedDisplay).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var allNamedKeys = nodes.SelectMany(m => queriesByMethod[m].Named).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var allEntity = nodes.SelectMany(m => queriesByMethod[m].Entity).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var allNative = nodes.SelectMany(m => queriesByMethod[m].Native).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            var allLoaders = nodes.SelectMany(m => queriesByMethod[m].DomainLoaders).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
            sb.AppendLine($"<title>{Html(title)}</title>");
            sb.AppendLine(@"<style>
body { font-family: Segoe UI, Roboto, Arial, sans-serif; margin: 24px; }
h1,h2,h3 { margin: 0.3em 0; }
small.muted { color: #666; }
.card { border:1px solid #ddd; border-radius:8px; padding:14px; margin-bottom:16px; }
.grid { display:grid; grid-template-columns: repeat(auto-fit,minmax(220px,1fr)); gap:12px; }
.kv { background:#fafafa; padding:10px; border:1px solid #eee; border-radius:6px; }
code, pre { font-family: Consolas, Menlo, monospace; }
pre.code { background:#0b1829; color:#d6e7ff; padding:12px; border-radius:8px; overflow:auto; }
summary { cursor:pointer; font-weight:600; }
.badge { display:inline-block; background:#eef3ff; border:1px solid #cfdcff; color:#2e4a9e; padding:2px 8px; border-radius:12px; margin-right:6px; font-size:12px; }
.table { width:100%; border-collapse: collapse; }
.table th, .table td { padding:8px 10px; border-bottom:1px solid #eee; text-align:left; }
.table th { background:#fafafa; }
.panel { margin-bottom:10px; }
.btn { display:inline-block; padding:6px 10px; border:1px solid #ccc; border-radius:6px; background:#f7f7f7; cursor:pointer; margin-right:8px; }
.btn:hover { background:#efefef; }
.term { color:#a33; font-weight:600; }
.chip { display:inline-block; padding:2px 10px; border-radius:999px; font-size:12px; margin-left:6px; border:1px solid #ddd; }
.chip.simple{ background:#f0fff0; color:#2f7d32; border-color:#cfe9cf; }
.chip.medium{ background:#fffbe6; color:#8a6d1d; border-color:#ebdea6; }
.chip.high{ background:#fff0f0; color:#b23a3a; border-color:#e5b6b6; }
.chip.veryhigh{ background:#ffe6ff; color:#7a1c7a; border-color:#e3b6e3; }
.smallcap { font-variant: small-caps; color:#555; }
hr.sep{ border:none; border-top:1px dashed #ddd; margin:12px 0; }
</style>");
            sb.AppendLine("</head><body>");

            // Header
            sb.AppendLine($"<h1>{Html(title)} <span class='chip {ComplexityScale.CssClass(funcLabel)}'>{Html(funcLabel)}</span></h1>");
            sb.AppendLine("<div class='card grid'>");
            sb.AppendLine(KV("Overall Cyclomatic (analyzed)", totalCyclomaticAnalyzed.ToString()));
            sb.AppendLine(KV("Total LOC (analyzed)", totalLOCAnalyzed.ToString()));
            sb.AppendLine(KV("Main method LOC", rootLOC.ToString()));
            sb.AppendLine(KV("Methods (analyzed)", analyzed.Count.ToString()));
            sb.AppendLine(KV("Terminal invocations (skipped)", terminals.Count.ToString()));
            sb.AppendLine("</div>");

            sb.AppendLine("<div style='margin:10px 0 16px 0'>");
            sb.AppendLine("<button class='btn' onclick=\"document.querySelectorAll('details').forEach(d=>d.open=true)\">Expand all</button>");
            sb.AppendLine("<button class='btn' onclick=\"document.querySelectorAll('details').forEach(d=>d.open=false)\">Collapse all</button>");
            sb.AppendLine("</div>");

            // Layer 2: Methods overview table
            sb.AppendLine("<h2>2) Methods overview</h2>");
            sb.AppendLine("<table class='table'>");
            sb.AppendLine("<thead><tr><th>#</th><th>Class</th><th>Method</th><th>LOC</th><th>Cyclomatic</th><th>Complexity</th><th>Entities</th></tr></thead><tbody>");
            int idx = 1;
            foreach (var m in nodes)
            {
                var ents = string.Join(", ", entitiesByMethod[m]);
                string loc = stopClasses.Contains(m.ClassName) ? "—" : m.Metrics.LOC.ToString();
                string cyc = stopClasses.Contains(m.ClassName) ? "—" : m.Metrics.Cyclomatic.ToString();
                var label = stopClasses.Contains(m.ClassName) ? "—" : ComplexityScale.MethodLabel(m.Metrics.Cyclomatic);
                var cls = stopClasses.Contains(m.ClassName) ? "" : ComplexityScale.CssClass(label);
                sb.AppendLine($"<tr><td>{idx++}</td><td>{Html(m.ClassName)}</td><td>{Html(m.Name)}</td><td>{loc}</td><td>{cyc}</td><td>{(label == "—" ? "—" : $"<span class='chip {cls}'>{Html(label)}</span>")}</td><td>{Html(ents)}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            // Layer 2.ii: per-method collapsible panels with queries
            sb.AppendLine("<h2>2.ii) Method details</h2>");
            foreach (var m in nodes)
            {
                var ents = entitiesByMethod[m];
                var entBadges = ents.Count == 0 ? "<span class='badge'>—</span>"
                                                : string.Join(" ", ents.Select(e => $"<span class='badge'>{Html(e)}</span>"));
                var label = stopClasses.Contains(m.ClassName) ? "—" : ComplexityScale.MethodLabel(m.Metrics.Cyclomatic);
                var cls = stopClasses.Contains(m.ClassName) ? "" : ComplexityScale.CssClass(label);
                var summaryRight = stopClasses.Contains(m.ClassName)
                    ? " <span class='term'>(terminal – analysis skipped)</span>"
                    : $" | LOC: <b>{m.Metrics.LOC}</b> | Cyclomatic: <b>{m.Metrics.Cyclomatic}</b> <span class='chip " + cls + "'>" + Html(label) + "</span>";

                var q = queriesByMethod[m];

                sb.AppendLine("<div class='panel card'>");
                sb.AppendLine($"<details><summary>{Html(m.ClassName)}.{Html(m.Name)}{summaryRight}</summary>");
                sb.AppendLine($"<div style='margin:8px 0 6px 0'><small class='muted'>{Html(m.FilePath)} : lines {m.StartLine}–{m.EndLine}</small></div>");
                sb.AppendLine($"<div style='margin:6px 0 10px 0'><b>Entities:</b> {entBadges}</div>");

                // Queries in this method
                sb.AppendLine("<div class='card'>");
                sb.AppendLine("<div class='smallcap'>Queries in this method</div><hr class='sep'/>");

                // Named queries (display with types + resolved text if available)
                if (q.NamedDisplay.Count == 0)
                {
                    sb.AppendLine("<div><b>Named queries:</b> —</div>");
                }
                else
                {
                    sb.AppendLine("<div><b>Named queries:</b><ul>");
                    foreach (var disp in q.NamedDisplay)
                    {
                        // find matching key (first Named key contained in disp)
                        var key = q.Named.FirstOrDefault(n => disp.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!string.IsNullOrEmpty(key) && q.NamedResolved.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text))
                            sb.AppendLine("<li><code>" + Html(disp) + "</code><div class='muted'><pre style='white-space:pre-wrap'>" + Html(text!) + "</pre></div></li>");
                        else
                            sb.AppendLine("<li><code>" + Html(disp) + "</code></li>");
                    }
                    sb.AppendLine("</ul></div>");
                }

                // Loaders (Domain-style) inside the named section per your request
                sb.AppendLine("<div><b>Loaders (Domain‑style):</b> " + (q.DomainLoaders.Count == 0 ? "—" : string.Join(", ", q.DomainLoaders.Select(Html))) + "</div><hr class='sep'/>");

                // Entity (HQL/LINQ/loaders typed)
                sb.AppendLine(SubQueryList("Entity queries", q.Entity));
                // Native SQL
                sb.AppendLine(SubQueryList("Native SQL", q.Native));
                sb.AppendLine("</div>");

                if (!stopClasses.Contains(m.ClassName))
                {
                    sb.AppendLine("<pre class='code'>");
                    foreach (var ln in ReadLines(m.FilePath, m.StartLine, m.EndLine))
                        sb.AppendLine(Html(ln));
                    sb.AppendLine("</pre>");
                }
                else
                {
                    sb.AppendLine("<div class='kv'>Traversal stops here (repository/security manager).</div>");
                }
                sb.AppendLine("</details>");
                sb.AppendLine("</div>");
            }

            // 2.iii terminal list
            if (terminals.Count > 0)
            {
                sb.AppendLine("<div class='card'>");
                sb.AppendLine("<h3>2.iii) Terminal invocations (not analyzed)</h3>");
                foreach (var t in terminals)
                    sb.AppendLine($"<div><span class='term'>{Html(t.ClassName)}.{Html(t.Name)}</span> <small class='muted'>— {Html(t.FilePath)}</small></div>");
                sb.AppendLine("</div>");
            }

            // Impacted queries (aggregated)
            sb.AppendLine("<h2>Impacted queries</h2>");
            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<div class='grid'>");
            sb.AppendLine(KV("Named queries", allNamed.Count.ToString()));
            sb.AppendLine(KV("Entity queries", allEntity.Count.ToString()));
            sb.AppendLine(KV("Native SQL", allNative.Count.ToString()));
            sb.AppendLine(KV("Loaders (Find/FindAll)", allLoaders.Count.ToString()));
            sb.AppendLine("</div>");

            // Named (with resolved text)
            sb.AppendLine("<div class='panel'><details open><summary><b>Named queries</b></summary>");
            if (allNamed.Count == 0) sb.AppendLine("<div>—</div>");
            else
            {
                sb.AppendLine("<ul>");
                foreach (var disp in allNamed)
                {
                    // find the query key that appears inside this display text
                    var key = allNamedKeys.FirstOrDefault(k => disp.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    var txt = key != null ? namedIndex.TryGetText(key) : null;
                    if (!string.IsNullOrWhiteSpace(txt))
                        sb.AppendLine("<li><code>" + Html(disp) + "</code><div class='muted'><pre style='white-space:pre-wrap'>" + Html(txt!) + "</pre></div></li>");
                    else
                        sb.AppendLine("<li><code>" + Html(disp) + "</code></li>");
                }
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</details></div>");

            sb.AppendLine("<div class='panel'><details><summary><b>Entity queries</b></summary>" + JoinBullets(allEntity) + "</details></div>");
            sb.AppendLine("<div class='panel'><details><summary><b>Native SQL</b></summary>" + JoinBullets(allNative) + "</details></div>");
            sb.AppendLine("<div class='panel'><details><summary><b>Loaders (Find/FindAll)</b></summary>" + JoinBullets(allLoaders) + "</details></div>");
            sb.AppendLine("</div>");

            // Layer 3: Work item summary
            sb.AppendLine("<h2>3) Work item summary</h2>");
            sb.AppendLine("<div class='card grid'>");
            sb.AppendLine(KV("Total LOC (analyzed methods)", totalLOCAnalyzed.ToString()));
            sb.AppendLine(KV("Impacted classes", impactedClasses.Count.ToString()));
            sb.AppendLine(KV("Impacted methods", impactedMethods.Count.ToString()));
            sb.AppendLine(KV("Impacted entities", impactedEntities.Count.ToString()));
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<b>Classes:</b><br/>" + string.Join(", ", impactedClasses.Select(Html)));
            sb.AppendLine("<hr/>");
            sb.AppendLine("<b>Methods:</b><br/>" + string.Join(", ", impactedMethods.Select(Html)));
            sb.AppendLine("<hr/>");
            sb.AppendLine("<b>Entities:</b><br/>" + (impactedEntities.Count == 0 ? "—" : string.Join(", ", impactedEntities.Select(Html))));
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='muted'><small>Note: Complexity/LOC excludes terminal repository/security managers and ignored utility calls. Report/DataSheet/DocumentTableRow adds are suppressed globally.</small></div>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string SubQueryList(string title, List<string> items)
        {
            if (items == null || items.Count == 0) return $"<div><b>{Html(title)}:</b> —</div>";
            var sb = new StringBuilder();
            sb.Append($"<div><b>{Html(title)}:</b><ul>");
            foreach (var s in items) sb.Append("<li><code>" + Html(s) + "</code></li>");
            sb.Append("</ul></div>");
            return sb.ToString();
        }

        private static string JoinBullets(List<string> items)
        {
            if (items.Count == 0) return "<div>—</div>";
            var sb = new StringBuilder("<ul>");
            foreach (var s in items) sb.Append("<li><code>" + Html(s) + "</code></li>");
            sb.Append("</ul>");
            return sb.ToString();
        }

        private static IEnumerable<string> ReadLines(string path, int start, int end)
        {
            var lines = File.ReadAllLines(path);
            for (int i = Math.Max(1, start) - 1; i < Math.Min(lines.Length, end); i++)
                yield return $"{i + 1,5}: {lines[i]}";
        }

        public static List<MethodDef> UniqueNodes(CallGraph g)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<MethodDef>();
            void Add(MethodDef m)
            {
                var key = m.FilePath + "|" + m.StartLine;
                if (set.Add(key)) list.Add(m);
            }
            Add(g.Root);
            foreach (var e in g.Edges) { Add(e.From); Add(e.To); }
            return list;
        }

        public static List<string> ExtractEntities(MethodDef m, IReadOnlyCollection<string> domainEntities)
        {
            var tokens = Regex.Matches(m.FullText, @"\b[A-Z][A-Za-z0-9_]*\b")
                              .Cast<Match>()
                              .Select(x => x.Value)
                              .Distinct(StringComparer.OrdinalIgnoreCase);

            var ent = tokens.Where(t => domainEntities.Contains(t, StringComparer.OrdinalIgnoreCase))
                            .Where(t => !EntityExclusions.Contains(t)) // explicit exclusions
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(s => s)
                            .ToList();
            return ent;
        }

        private static string KV(string k, string v) => $"<div class='kv'><div><b>{Html(k)}</b></div><div>{Html(v)}</div></div>";
        private static string Html(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }

    // ================== Dashboard (single HTML) ==================

    public static class DashboardBuilder
    {
        public static string Build(ConsolidatedMetrics cm, string title)
        {
            var groups = new Dictionary<string, List<EndpointMetrics>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Simple"] = new(),
                ["Medium"] = new(),
                ["High"] = new(),
                ["Very High"] = new()
            };
            foreach (var e in cm.Endpoints)
            {
                if (!groups.ContainsKey(e.Complexity)) groups[e.Complexity] = new();
                groups[e.Complexity].Add(e);
            }
            foreach (var k in groups.Keys.ToList())
                groups[k] = groups[k].OrderBy(x => x.Controller).ThenBy(x => x.Method).ToList();

            var json = JsonSerializer.Serialize(cm, new JsonSerializerOptions { WriteIndented = false });

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
            sb.AppendLine($"<title>{Html(title)}</title>");
            sb.AppendLine(@"<style>
body{font-family:Segoe UI,Roboto,Arial,sans-serif;margin:24px;}
h1,h2,h3{margin:0.3em 0;}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px;}
.card{border:1px solid #ddd;border-radius:10px;padding:14px;background:#fff;}
.badge{display:inline-block;background:#eef3ff;border:1px solid #cfdcff;color:#2e4a9e;padding:2px 8px;border-radius:12px;margin-right:6px;font-size:12px;}
.chip{display:inline-block;padding:2px 10px;border-radius:999px;font-size:12px;margin-left:6px;border:1px solid #ddd;}
.chip.simple{background:#f0fff0;color:#2f7d32;border-color:#cfe9cf;}
.chip.medium{background:#fffbe6;color:#8a6d1d;border-color:#ebdea6;}
.chip.high{background:#fff0f0;color:#b23a3a;border-color:#e5b6b6;}
.chip.veryhigh{background:#ffe6ff;color:#7a1c7a;border-color:#e3b6e3;}
.table{width:100%;border-collapse:collapse;}
.table th,.table td{padding:8px 10px;border-bottom:1px solid #eee;text-align:left;}
.table th{background:#fafafa;}
.btn{display:inline-block;padding:6px 10px;border:1px solid #ccc;border-radius:6px;background:#f7f7f7;cursor:pointer;margin-right:8px;}
.btn:hover{background:#efefef;}
.muted{color:#666;}
.summary{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px;margin-bottom:16px;}
.kv{background:#fafafa;padding:10px;border:1px solid #eee;border-radius:6px;}
code{font-family:Consolas,Menlo,monospace;}
details{margin-bottom:12px;}
</style>");
            sb.AppendLine(@"</head><body>");
            sb.AppendLine($"<h1>{Html(title)}</h1>");

            // Overall summary
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine(KV("Total endpoints", cm.Rollup?.TotalEndpoints.ToString() ?? "0"));
            sb.AppendLine(KV("Simple", cm.Rollup?.ByComplexity.GetValueOrDefault("Simple").ToString() ?? "0"));
            sb.AppendLine(KV("Medium", cm.Rollup?.ByComplexity.GetValueOrDefault("Medium").ToString() ?? "0"));
            sb.AppendLine(KV("High", cm.Rollup?.ByComplexity.GetValueOrDefault("High").ToString() ?? "0"));
            sb.AppendLine(KV("Very High", cm.Rollup?.ByComplexity.GetValueOrDefault("Very High").ToString() ?? "0"));
            sb.AppendLine("</div>");

            // Controls
            sb.AppendLine("<div style='margin:8px 0 16px 0'>");
            sb.AppendLine("<button class='btn' onclick='setView(\"cards\")'>Cards view</button>");
            sb.AppendLine("<button class='btn' onclick='setView(\"table\")'>Table view</button>");
            sb.AppendLine("</div>");

            // Cards view
            sb.AppendLine("<div id='cardsView'>");
            foreach (var key in new[] { "Simple", "Medium", "High", "Very High" })
            {
                var list = groups[key];
                var clsKey = ComplexityScale.CssClass(key);
                sb.AppendLine($"<details open><summary><b>{Html(key)}</b> <span class='muted'>({list.Count})</span></summary>");
                sb.AppendLine("<div class='grid'>");
                foreach (var e in list)
                {
                    sb.AppendLine("<div class='card'>");
                    sb.AppendLine($"<div><b><a href='{Html(e.OutputHtml ?? "#")}'>{Html(e.Controller)}.{Html(e.Method)}</a></b> <span class='chip {clsKey}'>{Html(e.Complexity)}</span>{(e.NotFound ? " <span class='badge'>NotFound</span>" : "")}</div>");
                    sb.AppendLine($"<div class='muted' style='font-size:12px'>{Html(Path.GetFileName(e.ControllerFile))}</div>");
                    sb.AppendLine("<div style='margin-top:8px' class='summary'>");
                    sb.AppendLine(KV("Total LOC", e.TotalLoc.ToString()));
                    sb.AppendLine(KV("# Methods", e.MethodsCount.ToString()));
                    sb.AppendLine(KV("# Classes", e.ClassesCount.ToString()));
                    sb.AppendLine("</div>");
                    sb.AppendLine("<details><summary><b>Impacted (methods & classes)</b></summary>");
                    sb.AppendLine("<div><b>Projects:</b> " + (e.ImpactedProjects.Count == 0 ? "—" : string.Join(", ", e.ImpactedProjects.Select(Html))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Classes:</b> " + (e.ImpactedClasses.Count == 0 ? "—" : string.Join(", ", e.ImpactedClasses.Select(Html))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Methods:</b> " + (e.ImpactedMethods.Count == 0 ? "—" : string.Join(", ", e.ImpactedMethods.Select(Html))) + "</div>");
                    sb.AppendLine("</details>");
                    sb.AppendLine("<details><summary><b>Entities & Queries</b></summary>");
                    sb.AppendLine("<div><b>Entities:</b> " + (e.ImpactedEntities.Count == 0 ? "—" : string.Join(", ", e.ImpactedEntities.Select(Html))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Named queries:</b><br/>" + (e.ImpactedQueries.Named.Count == 0 ? "—" : string.Join("<br/>", e.ImpactedQueries.Named.Select(s => "<code>" + Html(s) + "</code>"))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Entity queries:</b><br/>" + (e.ImpactedQueries.Entity.Count == 0 ? "—" : string.Join("<br/>", e.ImpactedQueries.Entity.Select(s => "<code>" + Html(s) + "</code>"))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Native SQL:</b><br/>" + (e.ImpactedQueries.Native.Count == 0 ? "—" : string.Join("<br/>", e.ImpactedQueries.Native.Select(s => "<code>" + Html(s) + "</code>"))) + "</div><hr/>");
                    sb.AppendLine("<div><b>Loaders (Find/FindAll):</b><br/>" + (e.ImpactedQueries.DomainLoaders.Count == 0 ? "—" : string.Join("<br/>", e.ImpactedQueries.DomainLoaders.Select(s => "<code>" + Html(s) + "</code>"))) + "</div>");
                    sb.AppendLine("</details>");
                    sb.AppendLine("</div>");
                }
                sb.AppendLine("</div>");
                sb.AppendLine("</details>");
            }
            sb.AppendLine("</div>"); // cardsView

            // Table view (with impacted classes & entities and link to endpoint HTML)
            sb.AppendLine("<div id='tableView' style='display:none'>");
            sb.AppendLine("<table class='table'><thead><tr>"
                + "<th>Endpoint</th><th>Complexity</th><th>Total LOC</th><th>#Methods</th><th>#Classes</th>"
                + "<th>Impacted Classes</th><th>Impacted Entities</th><th>Projects</th></tr></thead><tbody>");

            foreach (var e in cm.Endpoints.OrderBy(x => x.Complexity).ThenBy(x => x.Controller).ThenBy(x => x.Method))
            {
                var link = string.IsNullOrWhiteSpace(e.OutputHtml) ? "#" : e.OutputHtml;
                var proj = e.ImpactedProjects.Count == 0 ? "—" : string.Join(", ", e.ImpactedProjects.Select(Html));
                var cls = e.ImpactedClasses.Count == 0 ? "—" : string.Join(", ", e.ImpactedClasses.Select(Html));
                var ent = e.ImpactedEntities.Count == 0 ? "—" : string.Join(", ", e.ImpactedEntities.Select(Html));

                sb.AppendLine("<tr>"
                    + $"<td><a href='{Html(link)}'>{Html(e.Controller)}.{Html(e.Method)}</a>{(e.NotFound ? " <span class='badge'>NotFound</span>" : "")}</td>"
                    + $"<td>{Html(e.Complexity)}</td>"
                    + $"<td>{e.TotalLoc}</td>"
                    + $"<td>{e.MethodsCount}</td>"
                    + $"<td>{e.ClassesCount}</td>"
                    + $"<td style='max-width:360px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>{cls}</td>"
                    + $"<td style='max-width:360px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>{ent}</td>"
                    + $"<td>{proj}</td>"
                    + "</tr>");
            }
            sb.AppendLine("</tbody></table></div>");

            // Embed JSON (for future extensions) + view toggle
            sb.AppendLine("<script>window.METRICS = ");
            sb.AppendLine(json);
            sb.AppendLine(@";function setView(v){document.getElementById('cardsView').style.display=(v==='cards')?'block':'none';document.getElementById('tableView').style.display=(v==='table')?'block':'none';}</script>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string KV(string k, string v) => $"<div class='kv'><div><b>{Html(k)}</b></div><div>{Html(v)}</div></div>";
        private static string Html(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }

    // ================== Tuple comparer & Linq helpers ==================

    internal sealed class StringTupleComparer : IEqualityComparer<(string A, string B)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new();
        public bool Equals((string A, string B) x, (string A, string B) y)
            => string.Equals(x.A, y.A, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.B, y.B, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string A, string B) obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.A) * 397
               ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.B);
    }

    internal static class LinqEx
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seen.Add(keySelector(element)))
                    yield return element;
            }
        }
    }
}
