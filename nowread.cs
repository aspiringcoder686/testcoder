using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class XmlPropertyScraper
{
    static void Main()
    {
        string inputFolder = @"C:\YourXmlFolderPath"; // 🔑 Set your XML folder
        string outputTxtFile = @"C:\YourOutputPath\XmlPropertiesSummary.txt"; // 🔑 Text output
        string outputCsvFile = @"C:\YourOutputPath\XmlPropertiesSummary.csv"; // 🔑 CSV output

        var propertyAttributesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Collect properties and attributes
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

                    foreach (var attr in element.Attributes())
                    {
                        propertyAttributesMap[propertyName].Add(attr.Name.LocalName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }

        // Write TXT
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

        Console.WriteLine($"Done. TXT output: {outputTxtFile}");
        Console.WriteLine($"Done. CSV output: {outputCsvFile}");
    }
}
