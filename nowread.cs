 public static string Generate(EntityDefinition entity)
    {
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement("entity",
                new XAttribute("entity", entity.Name ?? string.Empty),
                new XAttribute("table", entity.Table ?? string.Empty),
                new XElement("id",
                    new XAttribute("column", entity.Properties.FirstOrDefault()?.Column ?? string.Empty)
                ),
                new XElement("properties",
                    entity.Properties.Select(p =>
                        new XElement("property",
                            new XAttribute("name", p.Name ?? string.Empty),
                            new XAttribute("column", p.Column ?? string.Empty),
                            new XAttribute("type", p.Type ?? "string")
                        ))
                ),
                new XElement("compositeKey",
                    entity.CompositeKey.Select(k =>
                        new XElement(k.Type == "many-to-one" ? "key-many-to-one" : "key-property",
                            new XAttribute("name", k.Name ?? string.Empty),
                            new XAttribute("column", k.Column ?? string.Empty),
                            k.Type == "many-to-one" ? new XAttribute("class", k.Class ?? string.Empty) : null
                        ))
                ),
                new XElement("relationships",
                    entity.Relationships.Select(r =>
                        new XElement("relationship",
                            new XAttribute("name", r.Name ?? string.Empty),
                            new XAttribute("type", r.Type ?? string.Empty),
                            new XAttribute("class", r.Class ?? string.Empty),
                            new XAttribute("column", r.Column ?? string.Empty)
                        ))
                ),
                new XElement("components",
                    entity.Components.Select(c =>
                        new XElement("component",
                            new XAttribute("name", c.Name ?? string.Empty),
                            new XAttribute("class", c.Class ?? string.Empty),
                            c.Properties.Select(p =>
                                new XElement("property",
                                    new XAttribute("name", p.Name ?? string.Empty),
                                    new XAttribute("column", p.Column ?? string.Empty),
                                    new XAttribute("type", p.Type ?? "string")
                                )
                            ).Concat(
                                c.Relationships.Select(r =>
                                    new XElement("relationship",
                                        new XAttribute("name", r.Name ?? string.Empty),
                                        new XAttribute("type", r.Type ?? string.Empty),
                                        new XAttribute("class", r.Class ?? string.Empty),
                                        new XAttribute("column", r.Column ?? string.Empty)
                                    )
                                )
                            )
                        )
                    )
                ),
                new XElement("queries",
                    entity.Queries.Select(q =>
                    {
                        var paramElements = new List<XElement>();
                        var namedParams = Regex.Matches(q.Sql ?? string.Empty, "[:@]([a-zA-Z_][a-zA-Z0-9_]*)");
                        foreach (Match match in namedParams)
                        {
                            var value = match.Groups[1].Value;
                            if (!string.IsNullOrWhiteSpace(value) && !paramElements.Any(p => p.Attribute("name")?.Value == value))
                            {
                                string type = value.ToLower().Contains("id") ? "guid" : "string";
                                paramElements.Add(new XElement("param",
                                    new XAttribute("name", value),
                                    new XAttribute("type", type)
                                ));
                            }
                        }

                        int questionCount = Regex.Matches(q.Sql ?? string.Empty, "\\?").Count;
                        for (int i = 1; i <= questionCount; i++)
                        {
                            paramElements.Add(new XElement("param",
                                new XAttribute("name", $"param{i}"),
                                new XAttribute("type", "string")));
                        }

                        return new XElement("query",
                            new XAttribute("name", q.Name ?? string.Empty),
                            new XElement("sql", ConvertToNativeSql(q.Sql, entity.Name, entity.Table, entity)),
                            paramElements.Any() ? new XElement("parameters", paramElements) : null
                        );
                    })
                )
            )
        );

        return doc.ToString();
    }
