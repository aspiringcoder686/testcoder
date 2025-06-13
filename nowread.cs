 // Default FindById query if ID present and not already added
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
     //var idProp = entity.Properties.First(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.Name.ToLower().Contains("id"));
     //whereClause = $"{idProp.Column} = @{idProp.Column}";
     entity.Queries.Add(new QueryDefinition
     {
         Name = "FindById",
         Sql = $"SELECT * FROM {entity.Table} WHERE {whereClause}"
     });
 }
