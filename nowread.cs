public class ClassInfo
{
    public string Key { get; set; }                      // The object id (capitalized)
    public string Type { get; set; }                     // The fully-qualified type name
    public int PropertyCount { get; set; }               // Number of properties defined
    public int DependencyCount { get; set; }             // Number of ref dependencies (constructor + property)
    public string Lifetime { get; set; }                 // "Singleton" or "Scoped"
    public string HasInlineObject { get; set; }          // "Yes" / "No"
    public string HasListOrDictionary { get; set; }      // "Yes" / "No"
    public int ConstructorArgCount { get; set; }         // Number of constructor args (value + ref)
    public double ComplexityScore { get; set; }          // Computed score based on our formula
    public string ComplexityCategory { get; set; }       // "Simple" / "Medium" / "Complex"
}


using ClosedXML.Excel;
using Newtonsoft.Json;

void GenerateExcel(Dictionary<string, object> appsettings, List<ClassInfo> classInfos)
{
    var wb = new XLWorkbook();

    // ---------------- CONFIGURATION SHEET -----------------
    var configSheet = wb.Worksheets.Add("Configuration");
    int configRow = 1;

    configSheet.Cell(configRow, 1).Value = "SNo";
    configSheet.Cell(configRow, 2).Value = "EntityName";
    configSheet.Cell(configRow, 3).Value = "Key";
    configSheet.Cell(configRow, 4).Value = "Value";
    configSheet.Range(configRow, 1, configRow, 4).Style.Font.Bold = true;

    configRow++;
    int configSno = 1;

    foreach (var entity in appsettings)
    {
        var entityName = entity.Key;
        if (entity.Value is Dictionary<string, object> props)
        {
            foreach (var prop in props)
            {
                configSheet.Cell(configRow, 1).Value = configSno;
                configSheet.Cell(configRow, 2).Value = entityName;
                configSheet.Cell(configRow, 3).Value = prop.Key;

                if (prop.Value is Dictionary<string, object> || prop.Value is List<object>)
                {
                    configSheet.Cell(configRow, 4).Value = JsonConvert.SerializeObject(prop.Value, Formatting.None);
                }
                else
                {
                    configSheet.Cell(configRow, 4).Value = prop.Value?.ToString();
                }

                configRow++;
                configSno++;
            }
        }
    }
    configSheet.Columns().AdjustToContents();

    // ---------------- METRICS SHEET -----------------
    var metricsSheet = wb.Worksheets.Add("Metrics");
    int metricsRow = 1;

    metricsSheet.Cell(metricsRow, 1).Value = "SNo";
    metricsSheet.Cell(metricsRow, 2).Value = "Key";
    metricsSheet.Cell(metricsRow, 3).Value = "Type";
    metricsSheet.Cell(metricsRow, 4).Value = "NoOfProperties";
    metricsSheet.Cell(metricsRow, 5).Value = "NoOfDependentClasses";
    metricsSheet.Cell(metricsRow, 6).Value = "SingletonOrScoped";
    metricsSheet.Cell(metricsRow, 7).Value = "HasInlineObject";
    metricsSheet.Cell(metricsRow, 8).Value = "HasListOrDictionary";
    metricsSheet.Cell(metricsRow, 9).Value = "ConstructorArgCount";
    metricsSheet.Cell(metricsRow, 10).Value = "ComplexityScore";
    metricsSheet.Cell(metricsRow, 11).Value = "ComplexityCategory";
    metricsSheet.Range(metricsRow, 1, metricsRow, 11).Style.Font.Bold = true;

    metricsRow++;
    int metricsSno = 1;

    foreach (var info in classInfos)
    {
        metricsSheet.Cell(metricsRow, 1).Value = metricsSno;
        metricsSheet.Cell(metricsRow, 2).Value = info.Key;
        metricsSheet.Cell(metricsRow, 3).Value = info.Type;
        metricsSheet.Cell(metricsRow, 4).Value = info.PropertyCount;
        metricsSheet.Cell(metricsRow, 5).Value = info.DependencyCount;
        metricsSheet.Cell(metricsRow, 6).Value = info.Lifetime;
        metricsSheet.Cell(metricsRow, 7).Value = info.HasInlineObject;
        metricsSheet.Cell(metricsRow, 8).Value = info.HasListOrDictionary;
        metricsSheet.Cell(metricsRow, 9).Value = info.ConstructorArgCount;
        metricsSheet.Cell(metricsRow, 10).Value = info.ComplexityScore;
        metricsSheet.Cell(metricsRow, 11).Value = info.ComplexityCategory;

        metricsRow++;
        metricsSno++;
    }
    metricsSheet.Columns().AdjustToContents();

    // ---------------- SAVE EXCEL -----------------
    wb.SaveAs("SpringConfigSummary.xlsx");
    Console.WriteLine("✅ SpringConfigSummary.xlsx written with Configuration + Metrics sheets.");
}
