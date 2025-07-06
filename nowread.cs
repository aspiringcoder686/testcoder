 var summaryWorkbook = new XLWorkbook();

 SummaryExporter.ExportSummary(summaryWorkbook, "Methods", new[] { "Library", "Namespace", "Method" },
     filteredMethods, r => new[] { r.Library, r.Namespace, r.Method });

 SummaryExporter.ExportSummary(summaryWorkbook, "Properties", new[] { "Library", "Namespace", "Property" },
     filteredProperties, r => new[] { r.Library, r.Namespace, r.Property });

 SummaryExporter.ExportSummary(summaryWorkbook, "Fields", new[] { "Library", "Namespace", "Field" },
     filteredFields, r => new[] { r.Library, r.Namespace, r.Field });

 SummaryExporter.ExportSummary(summaryWorkbook, "Events", new[] { "Library", "Namespace", "Event" },
     filteredEvents, r => new[] { r.Library, r.Namespace, r.Event });

 SummaryExporter.ExportSummary(summaryWorkbook, "Object Types", new[] { "Library", "Namespace", "Type" },
     filteredObjects, r => new[] { r.Library, r.Namespace, r.Type });

 SummaryExporter.ExportSummary(summaryWorkbook, "Attributes", new[] { "Library", "Namespace", "Attribute" },
     filteredAttributes, r => new[] { r.Library, r.Namespace, r.Attribute });

 SummaryExporter.ExportSummary(summaryWorkbook, "Interfaces", new[] { "Library", "Namespace", "Interface" },
     filteredInterfaces, r => new[] { r.Library, r.Namespace, r.Interface });

 summaryWorkbook.SaveAs("LibraryUsageSummary.xlsx");
 Console.WriteLine("Summary Excel file saved: LibraryUsageSummary.xlsx");


public static class SummaryExporter
{
    public static void ExportSummary<T>(
        XLWorkbook workbook,
        string sheetName,
        string[] headers,
        IEnumerable<T> items,
        Func<T, string[]> selector)
    {
        var uniqueRows = items
            .Select(selector)
            .Distinct(StringArrayComparer.Instance)
            .OrderBy(row => row[0]) // Sort by Library
            .ToList();

        var ws = workbook.Worksheets.Add(sheetName);

        // Write headers with S.No.
        ws.Cell(1, 1).Value = "S.No.";
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 2).Value = headers[i];

        // Write rows with S.No.
        for (int i = 0; i < uniqueRows.Count; i++)
        {
            var row = uniqueRows[i];
            ws.Cell(i + 2, 1).Value = i + 1;
            for (int j = 0; j < row.Length; j++)
                ws.Cell(i + 2, j + 2).Value = row[j];
        }

        ws.Columns().AdjustToContents();
    }

    private class StringArrayComparer : IEqualityComparer<string[]>
    {
        public static readonly StringArrayComparer Instance = new();
        public bool Equals(string[] x, string[] y) => x.SequenceEqual(y);
        public int GetHashCode(string[] obj) => string.Join("|", obj).GetHashCode();
    }
}
