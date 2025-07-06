
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DynamicAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFolder = @"C:\Path\To\Your\Project\Code"; // Replace with your .cs files path
            string binFolder = @"C:\Path\To\Your\Project\bin\Debug\net8.0"; // Replace with your bin path
            string configFile = "references.config"; // Optional: list of extra DLLs

            var csFiles = Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories);
            var syntaxTrees = csFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file))).ToList();

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            if (Directory.Exists(binFolder))
            {
                foreach (var dll in Directory.GetFiles(binFolder, "*.dll"))
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(dll));
                    }
                    catch { }
                }
            }

            if (File.Exists(configFile))
            {
                foreach (var dllPath in File.ReadAllLines(configFile))
                {
                    if (File.Exists(dllPath))
                    {
                        try
                        {
                            references.Add(MetadataReference.CreateFromFile(dllPath));
                        }
                        catch { }
                    }
                }
            }

            var compilation = CSharpCompilation.Create("Analysis", syntaxTrees, references);
            var results = new List<MethodCallInfo>();

            foreach (var tree in syntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        results.Add(new MethodCallInfo
                        {
                            Method = symbol.Name,
                            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
                            Library = symbol.ContainingAssembly?.Identity?.Name
                        });
                    }
                }
            }

            Console.WriteLine("Method\tNamespace\tLibrary");
            foreach (var r in results)
            {
                Console.WriteLine($"\{r.Method}\t\{r.Namespace}\t\{r.Library}");
            }
        }

        public class MethodCallInfo
        {
            public string Method { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
        }
    }
}
