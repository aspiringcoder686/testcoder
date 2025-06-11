CompositeKey = new List<CompositeKeyDefinition>(),
Components = new List<ComponentDefinition>(),
    
foreach (var comp in classElement.Elements(ns + "component"))
 {
     var compDef = new ComponentDefinition
     {
         Name = comp.Attribute("name")?.Value,
         Class = comp.Attribute("class")?.Value,
         Properties = new List<PropertyDefinition>(),
         Relationships = new List<RelationshipDefinition>()
     };

     foreach (var prop in comp.Elements(ns + "property"))
     {
         compDef.Properties.Add(new PropertyDefinition
         {
             Name = prop.Attribute("name")?.Value,
             Column = prop.Attribute("column")?.Value ?? prop.Attribute("name")?.Value,
             Type = prop.Attribute("type")?.Value ?? "string"
         });
     }

     foreach (var rel in comp.Elements(ns + "many-to-one"))
     {
         compDef.Relationships.Add(new RelationshipDefinition
         {
             Name = rel.Attribute("name")?.Value,
             Type = "many-to-one",
             Class = rel.Attribute("class")?.Value,
             Column = rel.Attribute("column")?.Value
         });
     }

     entity.Components.Add(compDef);
 }


public class ComponentDefinition
{
    public string Name { get; set; }
    public string Class { get; set; }
    public List<PropertyDefinition> Properties { get; set; } = new();
    public List<RelationshipDefinition> Relationships { get; set; } = new();
}
