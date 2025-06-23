var (classInfos, appsettings) = LoadSpringConfig(doc);

using System.Xml.Linq;
using System.Text.RegularExpressions;

public static (List<ClassInfo> classInfos, Dictionary<string, object> appsettings) LoadSpringConfig(XDocument doc)
{
    var classInfos = new List<ClassInfo>();
    var appsettings = new Dictionary<string, object>();

    foreach (var obj in doc.Descendants().Where(x => x.Name.LocalName == "object"))
    {
        var id = obj.Attribute("id")?.Value;
        var type = obj.Attribute("type")?.Value;
        var singleton = obj.Attribute("singleton")?.Value ?? "true";

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(type))
            continue; // skip anonymous / malformed

        var key = Capitalize(id);
        var classInfo = new ClassInfo
        {
            Key = key,
            Type = type,
            Lifetime = singleton == "true" ? "Singleton" : "Scoped",
            PropertyCount = 0,
            DependencyCount = 0,
            ConstructorArgCount = 0,
            HasInlineObject = "No",
            HasListOrDictionary = "No"
        };

        var propsDict = new Dictionary<string, object>();

        // Constructor args
        var ctorArgs = obj.Elements().Where(e => e.Name.LocalName == "constructor-arg").ToList();
        classInfo.ConstructorArgCount = ctorArgs.Count;
        classInfo.DependencyCount += ctorArgs.Count(e => !string.IsNullOrEmpty(e.Attribute("ref")?.Value));

        // Properties
        var props = obj.Elements().Where(e => e.Name.LocalName == "property").ToList();
        classInfo.PropertyCount = props.Count;

        foreach (var prop in props)
        {
            var propName = Capitalize(prop.Attribute("name")?.Value);
            var val = prop.Attribute("value")?.Value;
            var refVal = prop.Attribute("ref")?.Value;

            if (!string.IsNullOrEmpty(val))
                propsDict[propName] = val;

            if (!string.IsNullOrEmpty(refVal))
                classInfo.DependencyCount++;

            // Inline object
            var inlineObj = prop.Elements().FirstOrDefault(e => e.Name.LocalName == "object");
            if (inlineObj != null)
            {
                classInfo.HasInlineObject = "Yes";
                var inlineProps = ParseInlineObject(inlineObj);
                propsDict[propName] = inlineProps;
            }

            // Dictionary
            var dictElem = prop.Elements().FirstOrDefault(e => e.Name.LocalName == "dictionary");
            if (dictElem != null)
            {
                classInfo.HasListOrDictionary = "Yes";
                var dict = dictElem.Elements()
                    .Where(e => e.Name.LocalName == "entry")
                    .ToDictionary(
                        e => e.Attribute("key")?.Value,
                        e => e.Attribute("value")?.Value
                    );
                propsDict[propName] = dict;
            }

            // List
            var listElem = prop.Elements().FirstOrDefault(e => e.Name.LocalName == "list");
            if (listElem != null)
            {
                classInfo.HasListOrDictionary = "Yes";
                var list = listElem.Elements()
                    .Where(e => e.Name.LocalName == "value")
                    .Select(e => e.Value)
                    .ToList();
                propsDict[propName] = list;
            }
        }

        appsettings[key] = propsDict;

        // Compute complexity
        classInfo.ComplexityScore =
            classInfo.PropertyCount * 1.0 +
            classInfo.DependencyCount * 2.0 +
            classInfo.ConstructorArgCount * 1.5 +
            (classInfo.HasInlineObject == "Yes" ? 5.0 : 0.0) +
            (classInfo.HasListOrDictionary == "Yes" ? 5.0 : 0.0) +
            (classInfo.Lifetime == "Scoped" ? 1.0 : 0.0);

        classInfo.ComplexityCategory =
            classInfo.ComplexityScore < 15 ? "Simple" :
            classInfo.ComplexityScore < 25 ? "Medium" : "Complex";

        classInfos.Add(classInfo);
    }

    return (classInfos, appsettings);
}

// Helper functions
public static Dictionary<string, object> ParseInlineObject(XElement innerObj)
{
    var result = new Dictionary<string, object>();
    foreach (var prop in innerObj.Elements().Where(x => x.Name.LocalName == "property"))
    {
        var name = Capitalize(prop.Attribute("name")?.Value);
        var val = prop.Attribute("value")?.Value;
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(val))
            result[name] = val;
    }
    return result;
}

public static string Capitalize(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    return char.ToUpper(input[0]) + input.Substring(1);
}
