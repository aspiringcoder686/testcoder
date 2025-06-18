public static void MetricsGenerator(string filePath, string metricsPath)
 {
     string inputFolder = filePath; // 🔑 Set your XML folder
     string outputTxtFile = metricsPath + "\\XmlPropertiesSummary.txt"; // 🔑 Text output
     string outputCsvFile = metricsPath + "\\XmlPropertiesSummary.csv"; // 🔑 CSV output
     string outputAttrValueFile = metricsPath + "\\XmlPropertyAttributeValues.txt";

     var propertyAttributesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
     var propertyAttributeValuesMap = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

     var excludedAttrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "name", "column", "where", "class", "table", "assembly", "namespace" };

     foreach (var file in Directory.GetFiles(inputFolder, "*.xml", SearchOption.AllDirectories))
     {
         try
         {
             var doc = XDocument.Load(file);
             foreach (var element in doc.Descendants())
             {
                 var propertyName = element.Name.LocalName;
                 if (!propertyAttributesMap.ContainsKey(propertyName))
                     propertyAttributesMap[propertyName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                 if (!propertyAttributeValuesMap.ContainsKey(propertyName))
                     propertyAttributeValuesMap[propertyName] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                 foreach (var attr in element.Attributes())
                 {
                     // Add to attribute name list
                     propertyAttributesMap[propertyName].Add(attr.Name.LocalName);

                     // Add to attribute value list (if not excluded)
                     if (!excludedAttrs.Contains(attr.Name.LocalName))
                     {
                         if (!propertyAttributeValuesMap[propertyName].ContainsKey(attr.Name.LocalName))
                             propertyAttributeValuesMap[propertyName][attr.Name.LocalName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                         propertyAttributeValuesMap[propertyName][attr.Name.LocalName].Add(attr.Value);
                     }
                 }
             }
         }
         catch (Exception ex)
         {
             Console.WriteLine($"Error processing {file}: {ex.Message}");
         }
     }

     // Write TXT summary
     using (var writer = new StreamWriter(outputTxtFile))
     {
         foreach (var kvp in propertyAttributesMap.OrderBy(k => k.Key))
         {
             writer.WriteLine($"property: {kvp.Key}");
             writer.WriteLine($" attribute: {string.Join(", ", kvp.Value.OrderBy(a => a))}");
         }
     }

     // Write CSV
     using (var writer = new StreamWriter(outputCsvFile))
     {
         writer.WriteLine("SNo,Property,Attribute");
         int sno = 1;
         foreach (var kvp in propertyAttributesMap.OrderBy(k => k.Key))
         {
             foreach (var attr in kvp.Value.OrderBy(a => a))
             {
                 writer.WriteLine($"{sno},{kvp.Key},{attr}");
                 sno++;
             }
         }
     }

     // Write attribute values TXT
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

     Console.WriteLine($"Done. Summary TXT: {outputTxtFile}");
     Console.WriteLine($"Done. CSV: {outputCsvFile}");
     Console.WriteLine($"Done. Attribute Values TXT: {outputAttrValueFile}");
 }
