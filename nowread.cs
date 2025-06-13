public string PageUsedInWebsite { get; set; }
string aspxName = Path.GetFileNameWithoutExtension(file) + ".aspx";

string reactPath = Console.ReadLine();


var reactFiles = Directory.EnumerateFiles(reactPath, "*.*", SearchOption.AllDirectories)
    .Where(f => f.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
    .ToList();

foreach (var reactFile in reactFiles)
{
    var content = File.ReadAllText(reactFile);
    if (content.Contains(aspxName, StringComparison.OrdinalIgnoreCase))
    {
        usedInReact = true;
        break;
    }
}

fileResult.PageUsedInWebsite = usedInReact ? "Yes" : "No";

wsSummary.Cell(1, 6).Value = "Page Used In Website";
// ...
wsSummary.Cell(row, 6).Value = file.PageUsedInWebsite;
