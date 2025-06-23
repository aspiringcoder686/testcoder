static string Capitalize(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    return char.ToUpper(input[0]) + input.Substring(1);
}
static Dictionary<string, object> ParseInlineObject(XElement innerObj)
{
    var result = new Dictionary<string, object>();
    foreach (var prop in innerObj.Elements().Where(x => x.Name.LocalName == "property"))
    {
        var name = prop.Attribute("name")?.Value;
        var val = prop.Attribute("value")?.Value;

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(val))
        {
            result[Capitalize(name)] = val;
        }
    }
    return result;
}
