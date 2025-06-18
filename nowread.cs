public static EntityDefinition Parse(string path)
{
    XDocument doc = XDocument.Load(path);
    XNamespace ns = doc.Root.GetDefaultNamespace();

    XElement? hbmRoot = doc.Element(ns + "hibernate-mapping");
    string parentNamespace = hbmRoot?.Attribute("namespace")?.Value ?? "";

    XElement? classElement = doc.Descendants(ns + "class").FirstOrDefault();
    if (classElement == null) return null;

    string fullParentClass = classElement.Attribute("name")?.Value ?? "";

    EntityDefinition entity = new()
    {
        Name = fullParentClass.Split('.').Last(),
        Namespace = parentNamespace,
        Table = classElement.Attribute("table")?.Value,
        IdColumn = classElement.Element(ns + "id")?.Attribute("column")?.Value,
        Properties = [],
        CompositeKey = null,
        Components = null,
        Relationships = [],
        Queries = []
    };

    // ID
    XElement? idElement = classElement.Element(ns + "id");
    if (idElement != null)
    {
        entity.Properties.Add(new PropertyDefinition
        {
            Name = idElement.Attribute("name")?.Value ?? "Id",
            Column = idElement.Attribute("column")?.Value,
            Type = idElement.Attribute("type")?.Value ?? "string",
            IsPrimary = true
        });
    }

    // Composite ID
    XElement? compositeId = classElement.Element(ns + "composite-id");
    if (compositeId != null)
    {
        foreach (XElement keyProp in compositeId.Elements(ns + "key-property"))
        {
            entity.CompositeKey ??= [];
            entity.CompositeKey.Add(new CompositeKeyDefinition
            {
                Name = keyProp.Attribute("name")?.Value,
                Column = keyProp.Attribute("column")?.Value,
                Type = keyProp.Attribute("type")?.Value ?? "string"
            });
        }

        foreach (XElement keyRel in compositeId.Elements(ns + "key-many-to-one"))
        {
            entity.CompositeKey ??= [];
            entity.CompositeKey.Add(new CompositeKeyDefinition
            {
                Name = keyRel.Attribute("name")?.Value,
                Column = keyRel.Attribute("column")?.Value,
                Type = "many-to-one",
                Class = NormalizeClassName(keyRel.Attribute("class")?.Value, parentNamespace)
            });
        }
    }

    // Component
    foreach (XElement comp in classElement.Elements(ns + "component"))
    {
        entity.Components ??= [];
        ComponentDefinition compDef = new()
        {
            Name = comp.Attribute("name")?.Value,
            Class = comp.Attribute("class")?.Value,
            Properties = [],
            Relationships = [],
            Insert = comp.Attribute("insert")?.Value,
            Update = comp.Attribute("update")?.Value,
        };

        foreach (XElement prop in comp.Elements(ns + "property"))
        {
            compDef.Properties.Add(new PropertyDefinition
            {
                Name = prop.Attribute("name")?.Value,
                Column = prop.Attribute("column")?.Value ?? prop.Attribute("name")?.Value,
                Type = prop.Attribute("type")?.Value ?? "string"
            });
        }

        foreach (XElement rel in comp.Elements(ns + "many-to-one"))
        {
            compDef.Relationships.Add(new RelationshipDefinition
            {
                Name = rel.Attribute("name")?.Value,
                Type = "many-to-one",
                Class = NormalizeClassName(rel.Attribute("class")?.Value, parentNamespace),
                SourceColumn = rel.Attribute("column")?.Value
            });
        }

        entity.Components.Add(compDef);
    }

    // Properties
    foreach (XElement prop in classElement.Elements(ns + "property"))
    {
        entity.Properties.Add(new PropertyDefinition
        {
            Name = prop.Attribute("name")?.Value,
            Column = prop.Attribute("column")?.Value ?? prop.Attribute("name")?.Value,
            Type = prop.Attribute("type")?.Value ?? "string"
        });
    }

    // Relationships
    foreach (XElement rel in classElement.Elements())
    {
        if (rel.Name.LocalName == "many-to-one")
        {
            entity.Relationships.Add(new RelationshipDefinition
            {
                Name = rel.Attribute("name")?.Value,
                Type = "many-to-one",
                Class = NormalizeClassName(rel.Attribute("class")?.Value, parentNamespace),
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
                Class = NormalizeClassName(rel.Attribute("class")?.Value, parentNamespace),
                PropertyRef = rel.Attribute("property-ref")?.Value,
                Cascade = rel.Attribute("cascade")?.Value
            });
        }
        else if (rel.Name.LocalName == "bag")
        {
            RelationshipDefinition relDef = new()
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
                relDef.Class = NormalizeClassName(manyToMany.Attribute("class")?.Value, parentNamespace);
                relDef.InnerType = "many-to-many";
                relDef.DestinationColumn = manyToMany.Attribute("column")?.Value;
                relDef.NotFound = manyToMany.Attribute("not-found")?.Value;
            }
            else if (oneToMany != null)
            {
                relDef.Class = NormalizeClassName(oneToMany.Attribute("class")?.Value, parentNamespace);
                relDef.InnerType = "one-to-many";
                relDef.DestinationColumn = relDef.SourceColumn;
                relDef.NotFound = oneToMany.Attribute("not-found")?.Value;
            }

            entity.Relationships.Add(relDef);
        }
    }

    // Queries + FindById (same as your existing logic)
    if (!string.IsNullOrWhiteSpace(entity.Table))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = "ALL",
            Sql = $"SELECT * FROM {entity.Table}"
        });
    }

    foreach (XElement query in doc.Descendants(ns + "query"))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = SimplifyQueryName(query.Attribute("name")?.Value),
            Sql = XmlGenerator.ConvertToNativeSql(query.Value ?? "", entity.Name, entity.Table, entity)
                .Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim(),
            Cacheable = string.Equals(query.Attribute("cacheable")?.Value, "true", StringComparison.OrdinalIgnoreCase) ? true : null
        });
    }

    foreach (XElement query in doc.Descendants(ns + "sql-query"))
    {
        entity.Queries.Add(new QueryDefinition
        {
            Name = SimplifyQueryName(query.Attribute("name")?.Value),
            Sql = XmlGenerator.ConvertToNativeSql(query.Value ?? "", entity.Name, entity.Table, entity)
                .Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim(),
            Cacheable = string.Equals(query.Attribute("cacheable")?.Value, "true", StringComparison.OrdinalIgnoreCase) ? true : null,
            QueryType = "SqlQuery"
        });
    }

    bool hasId = entity.Properties.Any(p => p.IsPrimary == true)
                  || (entity.CompositeKey != null && entity.CompositeKey.Count != 0);

    bool hasFindById = entity.Queries.Any(q => q.Name.Equals("FindById", StringComparison.OrdinalIgnoreCase));

    if (hasId && !hasFindById && !string.IsNullOrEmpty(entity.Table))
    {
        string whereClause = "";
        if (entity.CompositeKey != null && entity.CompositeKey.Count != 0)
        {
            whereClause = string.Join(" AND ", entity.CompositeKey.Select(k => $"{k.Column} = @{k.Column}"));
        }
        else
        {
            var idProp = entity.Properties.First(p => p.IsPrimary == true);
            whereClause = $"{idProp.Column} = @Id";
        }

        entity.Queries.Add(new QueryDefinition
        {
            Name = "FindById",
            Sql = $"SELECT * FROM {entity.Table} WHERE {whereClause}"
        });
    }

    return entity;
}
private static string NormalizeClassName(string? className, string parentNamespace)
{
    if (string.IsNullOrWhiteSpace(className)) return string.Empty;
    if (className.Contains(".")) return className; 
    return string.IsNullOrWhiteSpace(parentNamespace) ? className : $"{parentNamespace}.{className}";
}
