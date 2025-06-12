Cacheable = string.Equals(query.Attribute("cacheable")?.Value, "true", StringComparison.OrdinalIgnoreCase) ? true : null

    relDef.Lazy = rel.Attribute(XName.Get("lazy"))?.Value;
                  relDef.Cascade = rel.Attribute(XName.Get("cascade"))?.Value;
                    relDef.Inverse = rel.Attribute(XName.Get("inverse"))?.Value;


  Insert = comp.Attribute("insert")?.Value,
  Update = comp.Attribute("update")?.Value,
    
    public class RelationshipDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }  // bag, many-to-one, etc.
    public string InnerType { get; set; }
    public string Class { get; set; }
    public string OneToManyClass { get; set; }
    public string ManyToManyClass { get; set; }
    public string Column { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Lazy { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Cascade { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Inverse { get; set; }
}

public class QueryDefinition
{
    public string Name { get; set; }
    public string Sql { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? Cacheable { get; set; }
}
