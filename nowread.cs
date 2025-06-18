public static EntityDefinition Parse(string path)
{
    var doc = XDocument.Load(path);
    XNamespace ns = doc.Root.GetDefaultNamespace();

    var hbmRoot = doc.Element(ns + "hibernate-mapping");
    string parentNamespace = hbmRoot?.Attribute("namespace")?.Value ?? "";

    var classElement = doc.Descendants(ns + "class").FirstOrDefault();
    if (classElement == null) return null;

    string fullParentClass = classElement.Attribute("name")?.Value ?? "";

    var entity = new EntityDefinition
    {
        Name = fullParentClass.Split('.').Last(),
        Table = classElement.Attribute("table")?.Value,
        Properties = new List<PropertyDefinition>(),
        CompositeKey = new List<CompositeKeyDefinition>(),
        Components = new List<ComponentDefinition>(),
        Relationships = new List<RelationshipDefinition>(),
        Queries = new List<QueryDefinition>(),
    };

    // ID
    var idElement = classElement.Element(ns + "id");
    if (idElement != null)
    {
        entity.Properties.Add(new PropertyDefinition
        {
            Name = idElement.Attribute("name")?.Value ?? "Id",
            Column = idElement.Attribute("column")?.Value,
            Type = idElement.Attribute("type")?.Value ?? "string"
        });
    }

    // Composite ID
    var compositeId = classElement.Element(ns + "composite-id");
    if (compositeId != null)
    {
        foreach (var keyProp in compositeId.Elements(ns + "key-property"))
        {
            entity.CompositeKey.Add(new CompositeKeyDefinition
            {
                Name = keyProp.Attribute("name")?.Value,
                Column = keyProp.Attribute("column")?.Value,
                Type = keyProp.Attribute("type")?.Value ?? "string"
            });
        }

        foreach (var keyRel in compositeId.Elements(ns + "key-many-to-one"))
        {
            entity.CompositeKey.Add(new CompositeKeyDefinition
            {
                Name = keyRel.Attribute("name")?.Value,
                Column = keyRel.Attribute("column")?.Value,
                Type = "many-to-one",
                Class = NormalizeClassName(keyRel.Attribute("class")?.Value, parentNamespace)
            });
        }
    }

    // Properties
    foreach (var prop in classElement.Elements(ns + "property"))
    {
        entity.Properties.Add(new PropertyDefinition
        {
            Name = prop.Attribute("name")?.Value,
            Column = prop.Attribute("column")?.Value ?? prop.Attribute("name")?.Value,
            Type = prop.Attribute("type")?.Value ?? "string"
        });
    }

    // Components
    foreach (var comp in classElement.Elements(ns + "component"))
    {
        var compDef = new ComponentDefinition
        {
            Name = comp.Attribute("name")?.Value,
            Class = comp.Attribute("class")?.Value,
            Properties = new List<PropertyDefinition>(),
            Relationships = new List<RelationshipDefinition>(),
            Insert = comp.Attribute("insert")?.Value,
            Update = comp.Attribute("update")?.Value,
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
                SourceColumn = rel.Attribute("column")?.Value
            });
        }

        entity.Components.Add(compDef);
    }

    // Relationships
    foreach (var rel in classElement.Elements())
    {
        if (rel.Name.LocalName == "many-to-one")
        {
            entity.Relationships.Add(new RelationshipDefinition
            {
                Name = rel.Attribute("name")?.Value,
                Type = "many-to-one",
                Class = rel.Attribute("class")?.Value,
                SourceColumn = rel.Attribute("column")?.Value,
                Cascade = rel.Attribute("cascade")?.Value,
                NotFound = rel.Attribute("not-found")?.Value
            });
        }
        else if (rel.Name.LocalName == "one-to-one")
        {
            entity.Relationships.Add(new RelationshipDefinition
            {
                Name = rel.Attribute("name")?.Value,
                Type = "one-to-one",
                Class = rel.Attribute("class")?.Value,
                PropertyRef = rel.Attribute("property-ref")?.Value,
                Cascade = rel.Attribute("cascade")?.Value
            });
        }
        else if (rel.Name.LocalName == "bag")
        {
            var relDef = new RelationshipDefinition
            {
                Name = rel.Attribute("name")?.Value,
                Type = "bag",
                Lazy = rel.Attribute("lazy")?.Value,
                Cascade = rel.Attribute("cascade")?.Value,
                Inverse = rel.Attribute("inverse")?.Value
            };

            var keyElement = rel.Element(ns + "key");
            relDef.SourceColumn = keyElement?.Attribute("column")?.Value;

            var oneToMany = rel.Element(ns + "one-to-many");
            var manyToMany = rel.Element(ns + "many-to-many");

            if (manyToMany != null)
            {
                string originalClass = manyToMany.Attribute("class")?.Value;
                relDef.Class = NormalizeClassName(originalClass, parentNamespace);
                relDef.InnerType = "many-to-many";
                //relDef.ManyToManyClass = relDef.Class;
                relDef.DestinationColumn = manyToMany.Attribute("column")?.Value;
                relDef.NotFound = manyToMany.Attribute("not-found")?.Value;
            }
            else if (oneToMany != null)
            {
                string originalClass = oneToMany.Attribute("class")?.Value;
                relDef.Class = NormalizeClassName(originalClass, parentNamespace);
                relDef.InnerType = "one-to-many";
                //relDef.OneToManyClass = relDef.Class;
                relDef.DestinationColumn = relDef.SourceColumn;
                relDef.NotFound = oneToMany.Attribute("not-found")?.Value;
            }

            entity.Relationships.Add(relDef);
        }
    }

    // Queries
    if (!string.IsNullOrWhiteSpace(entity.Table))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = "ALL",
            Sql = $"SELECT * FROM {entity.Table}"
        });
    }

    foreach (var query in doc.Descendants(ns + "query"))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = SimplifyQueryName(query.Attribute("name")?.Value),
            Sql = XmlGenerator.ConvertToNativeSql(query.Value ?? "", entity.Name, entity.Table, entity)
                  .Replace("\n", " ").Replace("\r", " ").Trim(),
            Cacheable = string.Equals(query.Attribute("cacheable")?.Value, "true", StringComparison.OrdinalIgnoreCase) ? true : null
        });
    }

    foreach (var query in doc.Descendants(ns + "sql-query"))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = SimplifyQueryName(query.Attribute("name")?.Value),
            Sql = XmlGenerator.ConvertToNativeSql(query.Value ?? "", entity.Name, entity.Table, entity)
                  .Replace("\n", " ").Replace("\r", " ").Trim()
        });
    }

    // FindById
    bool hasId = entity.Properties.Any(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.Name.ToLower().Contains("id"))
                 || entity.CompositeKey.Any();

    bool hasFindById = entity.Queries.Any(q => q.Name.Equals("FindById", StringComparison.OrdinalIgnoreCase));

    if (hasId && !hasFindById)
    {
        string whereClause = "";

        if (entity.CompositeKey.Any())
        {
            whereClause = string.Join(" AND ", entity.CompositeKey.Select(k => $"{k.Column} = @{k.Column}"));
        }
        else
        {
            var idProp = entity.Properties.First(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.Name.ToLower().Contains("id"));
            whereClause = $"{idProp.Column} = @{idProp.Column}";
        }

        entity.Queries.Add(new QueryDefinition
        {
            Name = "FindById",
            Sql = $"SELECT * FROM {entity.Table} WHERE {whereClause}"
        });
    }

    return entity;
}


  public class RelationshipDefinition
  {
      public string Name { get; set; }
      public string Type { get; set; }  // bag, many-to-one, etc.
      public string InnerType { get; set; }
      public string Class { get; set; }
     // public string OneToManyClass { get; set; }
      //public string ManyToManyClass { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string SourceColumn { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string DestinationColumn { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string Lazy { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string Cascade { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string Inverse { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string PropertyRef { get; set; }

      [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
      public string NotFound { get; set; }
  }
