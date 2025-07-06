
using ClosedXML.Excel;
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
            string sourceFolder = @"C:\Users\Admin\Downloads\DevopsVideo\NHibernateXmlSample2\NHibernateXmlSample"; // Replace with your .cs files path
            string binFolder = @"C:\Users\Admin\Downloads\DevopsVideo\NHibernateXmlSample2\NHibernateXmlSample\bin\Debug\net8.0"; // Replace with your bin path
            string configFile = "references.config"; // Optional: list of extra DLLs

            var csFiles = Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories);
            //var syntaxTrees = csFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file))).ToList();

            var syntaxTrees = csFiles
    .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file))
    .ToList();


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
            var methodResults = new List<MethodCallInfo>();
            var propertyResults = new List<PropertyAccessInfo>();

            var fieldResults = new List<FieldAccessInfo>();
            var eventResults = new List<EventAccessInfo>();
            var objectCreationResults = new List<ObjectCreationInfo>();
            var attributeResults = new List<AttributeUsageInfo>();
            var interfaceResults = new List<InterfaceImplInfo>();

            foreach (var tree in syntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
                string fileName = Path.GetFileName(tree.FilePath);
                foreach (var invocation in invocations)
                {
                    var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        methodResults.Add(new MethodCallInfo
                        {
                            Method = symbol.Name,
                            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
                            Library = symbol.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                }

                foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                {
                    var symbol = model.GetSymbolInfo(access).Symbol;
                    if (symbol is IPropertySymbol prop)
                    {
                        propertyResults.Add(new PropertyAccessInfo
                        {
                            Property = prop.Name,
                            Namespace = prop.ContainingNamespace?.ToDisplayString(),
                            Library = prop.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                    else if (symbol is IFieldSymbol field)
                    {
                        fieldResults.Add(new FieldAccessInfo
                        {
                            Field = field.Name,
                            Namespace = field.ContainingNamespace?.ToDisplayString(),
                            Library = field.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                    else if (symbol is IEventSymbol evt)
                    {
                        eventResults.Add(new EventAccessInfo
                        {
                            Event = evt.Name,
                            Namespace = evt.ContainingNamespace?.ToDisplayString(),
                            Library = evt.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                }

                // Object Creation
                foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    var symbol = model.GetSymbolInfo(creation).Symbol as IMethodSymbol;
                    var type = symbol?.ContainingType;
                    if (type != null)
                    {
                        objectCreationResults.Add(new ObjectCreationInfo
                        {
                            Type = type.Name,
                            Namespace = type.ContainingNamespace?.ToDisplayString(),
                            Library = type.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                }

                // Attribute Usage
                foreach (var attr in root.DescendantNodes().OfType<AttributeSyntax>())
                {
                    var symbol = model.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                    var attrClass = symbol?.ContainingType;
                    if (attrClass != null)
                    {
                        attributeResults.Add(new AttributeUsageInfo
                        {
                            Attribute = attrClass.Name,
                            Namespace = attrClass.ContainingNamespace?.ToDisplayString(),
                            Library = attrClass.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                }

                // Interface Implementations
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var classSymbol = model.GetDeclaredSymbol(classDecl);
                    if (classSymbol == null) continue;

                    foreach (var iface in classSymbol.Interfaces)
                    {
                        interfaceResults.Add(new InterfaceImplInfo
                        {
                            Interface = iface.Name,
                            Namespace = iface.ContainingNamespace?.ToDisplayString(),
                            Library = iface.ContainingAssembly?.Identity?.Name,
                            FileName = fileName
                        });
                    }
                }

            }

            //Console.WriteLine("--- Methods Used ---");
            //Console.WriteLine("Library\tNamespace\tMethod\tFileName");
            //foreach (var r in methodResults.OrderBy(r => r.Library ?? ""))
            //{
            //    Console.WriteLine($"{r.Library}\t{r.Namespace}\t{r.Method}\t{r.FileName}");
            //}

            //// OUTPUT PROPERTY RESULTS
            //Console.WriteLine("\n--- Properties Used ---");
            //Console.WriteLine("Library\tNamespace\tProperty\tFileName");
            //foreach (var p in propertyResults.OrderBy(p => p.Library ?? ""))
            //{
            //    Console.WriteLine($"{p.Library}\t{p.Namespace}\t{p.Property}\t{p.FileName}");
            //}

            string[] libraryFilters = { "NHibernate", "System.Text.Json" }; // Can be empty

            var filteredMethods = FilterHelper.FilterAndSort(methodResults, libraryFilters);
            var filteredProperties = FilterHelper.FilterAndSort(propertyResults, libraryFilters);
            var filteredFields = FilterHelper.FilterAndSort(fieldResults, libraryFilters);
            var filteredEvents = FilterHelper.FilterAndSort(eventResults, libraryFilters);
            var filteredObjects = FilterHelper.FilterAndSort(objectCreationResults, libraryFilters);
            var filteredAttributes = FilterHelper.FilterAndSort(attributeResults, libraryFilters);
            var filteredInterfaces = FilterHelper.FilterAndSort(interfaceResults, libraryFilters);

            // Console Output
            PrintHelper.Print("Methods Used", "Library\tNamespace\tMethod\tFileName", filteredMethods, r => $"{r.Library}\t{r.Namespace}\t{r.Method}\t{r.FileName}");
            PrintHelper.Print("Properties Used", "Library\tNamespace\tProperty\tFileName", filteredProperties, r => $"{r.Library}\t{r.Namespace}\t{r.Property}\t{r.FileName}");
            PrintHelper.Print("Fields Used", "Library\tNamespace\tField\tFileName", filteredFields, r => $"{r.Library}\t{r.Namespace}\t{r.Field}\t{r.FileName}");
            PrintHelper.Print("Events Used", "Library\tNamespace\tEvent\tFileName", filteredEvents, r => $"{r.Library}\t{r.Namespace}\t{r.Event}\t{r.FileName}");
            PrintHelper.Print("Object Creations", "Library\tNamespace\tType\tFileName", filteredObjects, r => $"{r.Library}\t{r.Namespace}\t{r.Type}\t{r.FileName}");
            PrintHelper.Print("Attributes Used", "Library\tNamespace\tAttribute\tFileName", filteredAttributes, r => $"{r.Library}\t{r.Namespace}\t{r.Attribute}\t{r.FileName}");
            PrintHelper.Print("Interfaces Used", "Library\tNamespace\tInterface\tFileName", filteredInterfaces, r => $"{r.Library}\t{r.Namespace}\t{r.Interface}\t{r.FileName}");

            // Excel Export
            var workbook = new XLWorkbook();
            ExcelExporter.ExportToExcel(workbook, "Methods Used", new[] { "Library", "Namespace", "Method", "FileName" }, filteredMethods, r => new[] { r.Library, r.Namespace, r.Method, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Properties Used", new[] { "Library", "Namespace", "Property", "FileName" }, filteredProperties, r => new[] { r.Library, r.Namespace, r.Property, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Fields Used", new[] { "Library", "Namespace", "Field", "FileName" }, filteredFields, r => new[] { r.Library, r.Namespace, r.Field, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Events Used", new[] { "Library", "Namespace", "Event", "FileName" }, filteredEvents, r => new[] { r.Library, r.Namespace, r.Event, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Object Creations", new[] { "Library", "Namespace", "Type", "FileName" }, filteredObjects, r => new[] { r.Library, r.Namespace, r.Type, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Attributes Used", new[] { "Library", "Namespace", "Attribute", "FileName" }, filteredAttributes, r => new[] { r.Library, r.Namespace, r.Attribute, r.FileName });
            ExcelExporter.ExportToExcel(workbook, "Interfaces Used", new[] { "Library", "Namespace", "Interface", "FileName" }, filteredInterfaces, r => new[] { r.Library, r.Namespace, r.Interface, r.FileName });

            workbook.SaveAs("LibraryUsageReport.xlsx");
        }

        public static void Print<T>(string title, string header, IEnumerable<T> items, Func<T, string> formatter)
        {
            Console.WriteLine($"\n--- {title} ---");
            Console.WriteLine(header);
            foreach (var item in items)
            {
                Console.WriteLine(formatter(item));
            }
        }





        public class MethodCallInfo
        {
            public string Method { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class PropertyAccessInfo
        {
            public string Property { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class FieldAccessInfo
        {
            public string Field { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class EventAccessInfo
        {
            public string Event { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class ObjectCreationInfo
        {
            public string Type { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class AttributeUsageInfo
        {
            public string Attribute { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }

        public class InterfaceImplInfo
        {
            public string Interface { get; set; }
            public string Namespace { get; set; }
            public string Library { get; set; }
            public string FileName { get; set; }
        }


        public static class FilterHelper
        {
            public static IEnumerable<T> FilterAndSort<T>(IEnumerable<T> items, string[] libraryFilters)
            {
                var prop = typeof(T).GetProperty("Library");

                var filtered = (libraryFilters.Length == 0)
                    ? items
                    : items.Where(item =>
                    {
                        var library = prop?.GetValue(item)?.ToString() ?? "";
                        return libraryFilters.Any(f => library.Contains(f, StringComparison.OrdinalIgnoreCase));
                    });

                return filtered.OrderBy(item => prop?.GetValue(item));
            }
        }

        public static class ExcelExporter
        {
            public static void ExportToExcel<T>(
                XLWorkbook workbook,
                string sheetName,
                string[] headers,
                IEnumerable<T> items,
                Func<T, string[]> rowSelector)
            {
                var ws = workbook.Worksheets.Add(sheetName);

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];

                int row = 2;
                foreach (var item in items)
                {
                    var values = rowSelector(item);
                    for (int col = 0; col < values.Length; col++)
                        ws.Cell(row, col + 1).Value = values[col];
                    row++;
                }

                ws.Columns().AdjustToContents();
            }


        }

        public static class PrintHelper
        {
            public static void Print<T>(string title, string header, IEnumerable<T> items, Func<T, string> formatter)
            {
                Console.WriteLine($"\n--- {title} ---");
                Console.WriteLine(header);
                foreach (var item in items)
                {
                    Console.WriteLine(formatter(item));
                }
            }
        }

    }
}
