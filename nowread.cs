var innerObj = prop.Elements().FirstOrDefault(e => e.Name.LocalName == "object");
                    if (innerObj != null)
                    {
                        var inlineProps = ParseInlineObject(innerObj);
                        AddToAppSettings(appsettings, id, name, inlineProps);
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
            result[name] = val;
        }
    }
    return result;
}

static void AddToAppSettings(Dictionary<string, object> appsettings, string objId, string propName, object value)
{
    if (string.IsNullOrEmpty(objId)) return;

    if (!appsettings.ContainsKey(objId))
        appsettings[objId] = new Dictionary<string, object>();

    var section = appsettings[objId] as Dictionary<string, object>;
    section[propName] = value;
}
