
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ClosedXML.Excel;  // NuGet: Install-Package ClosedXML

namespace AspxCsAnalyzer
{
    class FileAnalysisResult
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int TotalMethods { get; set; }
        public int AjaxMethods { get; set; }
        public int QueryStringUses { get; set; }
        public int ParamUses { get; set; }
    }

    class MethodDetail
    {
        public string FileName { get; set; }
        public string MethodName { get; set; }
        public string IsAjaxMethod { get; set; }
        public string UsesParameter { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter project directory path:");
            string projectPath = Console.ReadLine();

            if (!Directory.Exists(projectPath))
            {
                Console.WriteLine("Invalid path.");
                return;
            }

            var files = Directory.GetFiles(projectPath, "*.aspx.cs", SearchOption.AllDirectories);
            var fileResults = new List<FileAnalysisResult>();
            var methodDetails = new List<MethodDetail>();

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);

                var fileResult = new FileAnalysisResult
                {
                    FileName = Path.GetFileName(file),
                    FilePath = file,
                    TotalMethods = Regex.Matches(content, @"(public|private|protected|internal)\s+(static\s+)?\S+\s+\S+\s*\(.*?\)\s*{").Count,
                    AjaxMethods = Regex.Matches(content, @"\[AjaxMethod\]", RegexOptions.IgnoreCase).Count,
                    QueryStringUses = Regex.Matches(content, @"Request\.QueryString", RegexOptions.IgnoreCase).Count,
                    ParamUses = Regex.Matches(content, @"Request\.(QueryString|Params|Form|Cookies|ServerVariables)", RegexOptions.IgnoreCase).Count
                };

                fileResults.Add(fileResult);

                // Method-level detail
                var methodRegex = new Regex(@"(public|private|protected|internal)\s+(static\s+)?\S+\s+(?<name>\S+)\s*\(.*?\)\s*{");
                var ajaxRegex = new Regex(@"\[AjaxMethod\]", RegexOptions.IgnoreCase);
                var paramRegex = new Regex(@"Request\.(QueryString|Params|Form|Cookies|ServerVariables)", RegexOptions.IgnoreCase);

                var methodMatches = methodRegex.Matches(content);

                foreach (Match m in methodMatches)
                {
                    string methodName = m.Groups["name"].Value;

                    var beforeMethod = content.Substring(0, m.Index);
                    bool isAjax = ajaxRegex.Matches(beforeMethod).Count > 0;

                    bool usesParam = false;
                    int braceIndex = content.IndexOf('{', m.Index);
                    if (braceIndex >= 0)
                    {
                        int level = 1;
                        int i = braceIndex + 1;
                        while (i < content.Length && level > 0)
                        {
                            if (content[i] == '{') level++;
                            else if (content[i] == '}') level--;
                            i++;
                        }

                        if (i > braceIndex)
                        {
                            var methodBody = content.Substring(braceIndex, i - braceIndex);
                            usesParam = paramRegex.IsMatch(methodBody);
                        }
                    }

                    methodDetails.Add(new MethodDetail
                    {
                        FileName = Path.GetFileName(file),
                        MethodName = methodName,
                        IsAjaxMethod = isAjax ? "Yes" : "No",
                        UsesParameter = usesParam ? "Yes" : "No"
                    });
                }
            }

            GenerateExcel(fileResults, methodDetails);
            Console.WriteLine("✅ Report generated: AspxCsAnalysisReport.xlsx");
        }

        static void GenerateExcel(List<FileAnalysisResult> fileResults, List<MethodDetail> methodDetails)
        {
            using (var workbook = new XLWorkbook())
            {
                // Sheet 1: Summary
                var wsSummary = workbook.Worksheets.Add("Analysis");

                wsSummary.Cell(1, 1).Value = "File";
                wsSummary.Cell(1, 2).Value = "Total Methods";
                wsSummary.Cell(1, 3).Value = "AjaxMethod Methods";
                wsSummary.Cell(1, 4).Value = "QueryString Uses";
                wsSummary.Cell(1, 5).Value = "Param Uses";

                int row = 2;
                foreach (var file in fileResults)
                {
                    wsSummary.Cell(row, 1).Value = file.FileName;
                    wsSummary.Cell(row, 2).Value = file.TotalMethods;
                    wsSummary.Cell(row, 3).Value = file.AjaxMethods;
                    wsSummary.Cell(row, 4).Value = file.QueryStringUses;
                    wsSummary.Cell(row, 5).Value = file.ParamUses;
                    row++;
                }

                wsSummary.Cell(row, 1).Value = "TOTAL";
                wsSummary.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row - 1})";
                wsSummary.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row - 1})";
                wsSummary.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row - 1})";
                wsSummary.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";

                wsSummary.Columns().AdjustToContents();

                // Sheet 2: Methods
                var wsMethods = workbook.Worksheets.Add("Methods");
                wsMethods.Cell(1, 1).Value = "FileName";
                wsMethods.Cell(1, 2).Value = "MethodName";
                wsMethods.Cell(1, 3).Value = "IsAjaxMethod";
                wsMethods.Cell(1, 4).Value = "UsesParameter";

                int methodRow = 2;
                foreach (var md in methodDetails)
                {
                    wsMethods.Cell(methodRow, 1).Value = md.FileName;
                    wsMethods.Cell(methodRow, 2).Value = md.MethodName;
                    wsMethods.Cell(methodRow, 3).Value = md.IsAjaxMethod;
                    wsMethods.Cell(methodRow, 4).Value = md.UsesParameter;
                    methodRow++;
                }

                wsMethods.Columns().AdjustToContents();

                workbook.SaveAs("AspxCsAnalysisReport.xlsx");
            }
        }
    }
}
