using System.Collections.Generic;
using System.Xml.Serialization;

namespace HbmToDapperConverter
{
    [XmlRoot("hibernate-mapping", Namespace = "urn:nhibernate-mapping-2.2")]
    public class HbmMapping
    {
        [XmlAttribute("namespace")]
        public string Namespace { get; set; }

        [XmlElement("class")]
        public HbmClass Class { get; set; }

        [XmlElement("query")]
        public List<HbmQuery> Queries { get; set; }

        [XmlElement("sql-query")]
        public List<HbmSqlQuery> SqlQueries { get; set; }
    }

    public class HbmClass
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("table")]
        public string Table { get; set; }

        [XmlElement("id")]
        public HbmId Id { get; set; }

        [XmlElement("composite-id")]
        public HbmCompositeId CompositeId { get; set; }

        [XmlElement("property")]
        public List<HbmProperty> Properties { get; set; }

        [XmlElement("component")]
        public List<HbmComponent> Components { get; set; }

        [XmlElement("many-to-one")]
        public List<HbmManyToOne> ManyToOnes { get; set; }

        [XmlElement("bag")]
        public List<HbmBag> Bags { get; set; }
    }

    public class HbmId
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class HbmCompositeId
    {
        [XmlElement("key-property")]
        public List<HbmKeyProperty> KeyProperties { get; set; }

        [XmlElement("key-many-to-one")]
        public List<HbmKeyManyToOne> KeyManyToOnes { get; set; }
    }

    public class HbmKeyProperty
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class HbmKeyManyToOne
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }

        [XmlAttribute("class")]
        public string Class { get; set; }
    }

    public class HbmProperty
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class HbmComponent
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("class")]
        public string Class { get; set; }

        [XmlAttribute("insert")]
        public string Insert { get; set; }

        [XmlAttribute("update")]
        public string Update { get; set; }

        [XmlElement("property")]
        public List<HbmProperty> Properties { get; set; }

        [XmlElement("many-to-one")]
        public List<HbmManyToOne> ManyToOnes { get; set; }
    }

    public class HbmManyToOne
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("class")]
        public string Class { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }
    }

    public class HbmBag
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("lazy")]
        public string Lazy { get; set; }

        [XmlAttribute("cascade")]
        public string Cascade { get; set; }

        [XmlAttribute("inverse")]
        public string Inverse { get; set; }

        [XmlElement("key")]
        public HbmKey Key { get; set; }

        [XmlElement("one-to-many")]
        public HbmOneToMany OneToMany { get; set; }

        [XmlElement("many-to-many")]
        public HbmManyToMany ManyToMany { get; set; }
    }

    public class HbmKey
    {
        [XmlAttribute("column")]
        public string Column { get; set; }
    }

    public class HbmOneToMany
    {
        [XmlAttribute("class")]
        public string Class { get; set; }
    }

    public class HbmManyToMany
    {
        [XmlAttribute("class")]
        public string Class { get; set; }

        [XmlAttribute("column")]
        public string Column { get; set; }
    }

    public class HbmQuery
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("cacheable")]
        public string Cacheable { get; set; }

        [XmlText]
        public string Sql { get; set; }
    }

    public class HbmSqlQuery
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Sql { get; set; }
    }
}


public static class HbmParser
{
    public static EntityDefinition Parse(string path)
    {
        var serializer = new XmlSerializer(typeof(HbmMapping));
        using var reader = new StreamReader(path);
        var hbm = (HbmMapping)serializer.Deserialize(reader);
        if (hbm?.Class == null) return null;

        var entity = new EntityDefinition
        {
            Name = hbm.Class.Name.Split('.').Last(),
            Table = hbm.Class.Table,
            Properties = new(),
            CompositeKey = new(),
            Components = new(),
            Relationships = new(),
            Queries = new(),
        };

        // <id>
        if (hbm.Class.Id != null)
        {
            entity.Properties.Add(new PropertyDefinition
            {
                Name = hbm.Class.Id.Name,
                Column = hbm.Class.Id.Column,
                Type = hbm.Class.Id.Type ?? "string"
            });
        }

        // <composite-id>
        if (hbm.Class.CompositeId != null)
        {
            foreach (var kp in hbm.Class.CompositeId.KeyProperties ?? [])
            {
                entity.CompositeKey.Add(new CompositeKeyDefinition
                {
                    Name = kp.Name,
                    Column = kp.Column,
                    Type = kp.Type ?? "string"
                });
            }
            foreach (var km in hbm.Class.CompositeId.KeyManyToOnes ?? [])
            {
                entity.CompositeKey.Add(new CompositeKeyDefinition
                {
                    Name = km.Name,
                    Column = km.Column,
                    Type = "many-to-one",
                    Class = NormalizeClassName(km.Class, hbm.Namespace)
                });
            }
        }

        // <property>
        foreach (var p in hbm.Class.Properties ?? [])
        {
            entity.Properties.Add(new PropertyDefinition
            {
                Name = p.Name,
                Column = p.Column ?? p.Name,
                Type = p.Type ?? "string"
            });
        }

        // <component>
        foreach (var c in hbm.Class.Components ?? [])
        {
            var compDef = new ComponentDefinition
            {
                Name = c.Name,
                Class = c.Class,
                Insert = c.Insert,
                Update = c.Update
            };

            foreach (var cp in c.Properties ?? [])
            {
                compDef.Properties.Add(new PropertyDefinition
                {
                    Name = cp.Name,
                    Column = cp.Column ?? cp.Name,
                    Type = cp.Type ?? "string"
                });
            }

            foreach (var cr in c.ManyToOnes ?? [])
            {
                compDef.Relationships.Add(new RelationshipDefinition
                {
                    Name = cr.Name,
                    Type = "many-to-one",
                    Class = cr.Class,
                    Column = cr.Column
                });
            }

            entity.Components.Add(compDef);
        }

        // <many-to-one>
        foreach (var r in hbm.Class.ManyToOnes ?? [])
        {
            entity.Relationships.Add(new RelationshipDefinition
            {
                Name = r.Name,
                Type = "many-to-one",
                Class = NormalizeClassName(r.Class, hbm.Namespace),
                Column = r.Column
            });
        }

        // <bag>
        foreach (var b in hbm.Class.Bags ?? [])
        {
            var relDef = new RelationshipDefinition
            {
                Name = b.Name,
                Type = "bag",
                Lazy = b.Lazy,
                Cascade = b.Cascade,
                Inverse = b.Inverse,
                Column = b.Key?.Column
            };

            if (b.OneToMany != null)
            {
                var cls = NormalizeClassName(b.OneToMany.Class, hbm.Namespace);
                relDef.InnerType = "one-to-many";
                relDef.Class = cls;
                relDef.OneToManyClass = cls;
            }
            if (b.ManyToMany != null)
            {
                var cls = NormalizeClassName(b.ManyToMany.Class, hbm.Namespace);
                relDef.InnerType = "many-to-many";
                relDef.Class = cls;
                relDef.ManyToManyClass = cls;
            }

            entity.Relationships.Add(relDef);
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

        foreach (var q in hbm.Queries ?? [])
        {
            entity.Queries.Add(new QueryDefinition
            {
                Name = SimplifyQueryName(q.Name),
                Sql = q.Sql?.Trim(),
                Cacheable = q.Cacheable?.ToLower() == "true" ? true : null
            });
        }

        foreach (var q in hbm.SqlQueries ?? [])
        {
            entity.Queries.Add(new QueryDefinition
            {
                Name = SimplifyQueryName(q.Name),
                Sql = q.Sql?.Trim()
            });
        }

        // FindById default
        bool hasId = entity.Properties.Any(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.Name.ToLower().Contains("id"))
                     || entity.CompositeKey.Any();
        bool hasFindById = entity.Queries.Any(q => q.Name.Equals("FindById", StringComparison.OrdinalIgnoreCase));

        if (hasId && !hasFindById)
        {
            string whereClause = entity.CompositeKey.Any()
                ? string.Join(" AND ", entity.CompositeKey.Select(k => $"{k.Column} = @{k.Column}"))
                : $"{entity.Properties.First(p => p.Name.ToLower().Contains("id")).Column} = @{entity.Properties.First(p => p.Name.ToLower().Contains("id")).Column}";

            entity.Queries.Add(new QueryDefinition
            {
                Name = "FindById",
                Sql = $"SELECT * FROM {entity.Table} WHERE {whereClause}"
            });
        }

        return entity;
    }

    private static string SimplifyQueryName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? name : name.Split('.').Last();
    }

    private static string NormalizeClassName(string originalClass, string parentNamespace)
    {
        if (string.IsNullOrWhiteSpace(originalClass)) return null;
        if (originalClass.StartsWith("Business.AMS")) return originalClass;
        if (!originalClass.Contains('.')) return $"{parentNamespace}.{originalClass}";
        return originalClass;
    }
}
