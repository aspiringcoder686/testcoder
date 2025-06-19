 public static void MetricsGenerator(string filePath, string metricsPath)
    {
        string inputFolder = filePath;
        string outputTxtFile = Path.Combine(metricsPath, "XmlPropertiesSummary.txt");
        string outputCsvFile = Path.Combine(metricsPath, "XmlPropertiesSummary.csv");
        string outputAttrValueFile = Path.Combine(metricsPath, "XmlPropertyAttributeValues.txt");
        string outputExcelFile = Path.Combine(metricsPath, "XmlDynamicClassSummary.xlsx");

        var propertyAttributesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var propertyAttributeValuesMap = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

        var excludedAttrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "name", "column", "where", "class", "table", "assembly", "namespace" };

        var allElementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileSummaries = new List<Dictionary<string, string>>();

        int sno = 1;

        foreach (var file in Directory.GetFiles(inputFolder, "*.xml", SearchOption.AllDirectories))
        {
            try
            {
                var doc = XDocument.Load(file);

                var summary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                summary["SNo"] = sno.ToString();
                summary["FileName"] = Path.GetFileName(file);

                var hibernateMapping = doc.Root;
                summary["Namespace"] = hibernateMapping?.Attribute("namespace")?.Value ?? "";

                var classElement = hibernateMapping?.Element(hibernateMapping.GetDefaultNamespace() + "class");
                summary["ClassName"] = classElement?.Attribute("name")?.Value ?? "";
                summary["TableName"] = classElement?.Attribute("table")?.Value ?? "";

                var elementCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var element in doc.Descendants())
                {
                    var elemName = element.Name.LocalName;
                    allElementNames.Add(elemName);

                    if (!elementCounts.ContainsKey(elemName))
                        elementCounts[elemName] = 0;
                    elementCounts[elemName]++;

                    if (!propertyAttributesMap.ContainsKey(elemName))
                        propertyAttributesMap[elemName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    if (!propertyAttributeValuesMap.ContainsKey(elemName))
                        propertyAttributeValuesMap[elemName] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var attr in element.Attributes())
                    {
                        propertyAttributesMap[elemName].Add(attr.Name.LocalName);

                        if (!excludedAttrs.Contains(attr.Name.LocalName))
                        {
                            if (!propertyAttributeValuesMap[elemName].ContainsKey(attr.Name.LocalName))
                                propertyAttributeValuesMap[elemName][attr.Name.LocalName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            propertyAttributeValuesMap[elemName][attr.Name.LocalName].Add(attr.Value);
                        }
                    }
                }

                foreach (var kv in elementCounts)
                {
                    summary[kv.Key] = kv.Value.ToString();
                }

                fileSummaries.Add(summary);
                sno++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }

        // TXT summary
        using (var writer = new StreamWriter(outputTxtFile))
        {
            foreach (var kvp in propertyAttributesMap.OrderBy(k => k.Key))
            {
                writer.WriteLine($"property: {kvp.Key}");
                writer.WriteLine($" attribute: {string.Join(", ", kvp.Value.OrderBy(a => a))}");
            }
        }

        // CSV summary
        using (var writer = new StreamWriter(outputCsvFile))
        {
            writer.WriteLine("SNo,Property,Attribute");
            int csvSno = 1;
            foreach (var kvp in propertyAttributesMap.OrderBy(k => k.Key))
            {
                foreach (var attr in kvp.Value.OrderBy(a => a))
                {
                    writer.WriteLine($"{csvSno},{kvp.Key},{attr}");
                    csvSno++;
                }
            }
        }

        // Attribute value TXT
        using (var writer = new StreamWriter(outputAttrValueFile))
        {
            foreach (var kvp in propertyAttributeValuesMap.OrderBy(k => k.Key))
            {
                writer.WriteLine($"property: {kvp.Key}");
                foreach (var attrKvp in kvp.Value.OrderBy(a => a.Key))
                {
                    writer.WriteLine($" attribute: {attrKvp.Key}: {string.Join(", ", attrKvp.Value.OrderBy(v => v))}");
                }
            }
        }

        // Excel dynamic summary
        var columns = new List<string> { "SNo", "FileName", "ClassName", "Namespace", "TableName" };
        columns.AddRange(allElementNames.OrderBy(x => x));

        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("XML Summary");

            // Header
            for (int i = 0; i < columns.Count; i++)
            {
                ws.Cell(1, i + 1).Value = columns[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Data rows
            for (int rowIdx = 0; rowIdx < fileSummaries.Count; rowIdx++)
            {
                var summary = fileSummaries[rowIdx];
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    var col = columns[colIdx];

                    if (col == "SNo")
                    {
                        ws.Cell(rowIdx + 2, colIdx + 1).Value = int.Parse(summary["SNo"]);
                    }
                    else if (col == "FileName" || col == "ClassName" || col == "Namespace" || col == "TableName")
                    {
                        ws.Cell(rowIdx + 2, colIdx + 1).Value = summary.ContainsKey(col) ? summary[col] : "";
                    }
                    else
                    {
                        if (summary.ContainsKey(col) && int.TryParse(summary[col], out int numVal))
                        {
                            ws.Cell(rowIdx + 2, colIdx + 1).Value = numVal;
                        }
                        else
                        {
                            ws.Cell(rowIdx + 2, colIdx + 1).Value = 0;
                        }
                    }
                }
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(outputExcelFile);
        }

        Console.WriteLine($"Done. Summary TXT: {outputTxtFile}");
        Console.WriteLine($"Done. CSV: {outputCsvFile}");
        Console.WriteLine($"Done. Attribute Values TXT: {outputAttrValueFile}");
        Console.WriteLine($"Done. Excel: {outputExcelFile}");
    }
