var set = await ctx.AmBusinessRuleSets
    .Where(s => s.BusinessRuleSetGuid == guid)
    .Select(s => new BusinessRuleSet
    {
        Id = s.BusinessRuleSetGuid,
        Nickname = s.Nickname,
        BusinessRules = ctx.AmBusinessRules
            .Where(r => r.BusinessRuleSetGuid == s.BusinessRuleSetGuid)
            .Select(r => new BusinessRule
            {
                Code = r.Code,
                Name = r.Name,
                FundGroupsOverride = ctx.AmBusinessRuleFoundGroupOverrides
                    .Where(o => o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid)
                    .Join(ctx.AmRefFundGroups,
                          o => o.FundGroupShortName,
                          fg => fg.FundGroupShortName,
                          (o, fg) => new FundGroup { ShortName = fg.FundGroupShortName })
                    .ToList(),
                ModelTypes = ctx.AmBusinessRuleModelTypes
                    .Where(o => o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid)
                    .Join(ctx.AmRefAssetModelTypes,
                          o => o.AssetModelTypeCode,
                          mt => mt.AssetModelTypeCode,
                          (o, mt) => new AssetModelType { Code = mt.AssetModelTypeCode })
                    .ToList()
            }).ToList()
    })
    .SingleOrDefaultAsync();
