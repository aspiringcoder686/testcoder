public Task<T?> QuerySingleAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true);

public Task<List<T>> QueryListAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true);

public virtual async Task<T?> QuerySingleAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true)
{
    _db.ApplyAssetApprovedFilter = approvedOnly;        // <- already your pattern
    return await queryBuilder(_db)
        .AsNoTracking()
        .SingleOrDefaultAsync();
}

public virtual async Task<List<T>> QueryListAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true)
{
    _db.ApplyAssetApprovedFilter = approvedOnly;        // <- already your pattern
    return await queryBuilder(_db)
        .AsNoTracking()
        .ToListAsync();
}


// SecureEntityManager.cs
public override async Task<T?> QuerySingleAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true)
{
    _db.AllowedFundGroups = await securityManager.FilterListByAccess(); // ensure security filter
    return await base.QuerySingleAsync(queryBuilder, approvedOnly);
}

public override async Task<List<T>> QueryListAsync<T>(
    Func<AMSDbContext, IQueryable<T>> queryBuilder,
    bool approvedOnly = true)
{
    _db.AllowedFundGroups = await securityManager.FilterListByAccess(); // ensure security filter
    return await base.QueryListAsync(queryBuilder, approvedOnly);
}



// Query builder (can live in a static class for reuse)
private static IQueryable<BusinessRuleSet> BusinessRuleSetByGuidQuery(AMSDbContext ctx, Guid guid)
{
    // NOTE: using ctx.Set<T>() keeps this generic & testable
    var sets = ctx.Set<AmBusinessRuleSet>()
        .Where(s => s.BusinessRuleSetGuid == guid);

    return sets.Select(s => new BusinessRuleSet
    {
        Id                   = s.BusinessRuleSetGuid,
        TemplateGUID         = s.TemplateGuid,
        Active               = s.Active,
        Final                = s.Final,
        Nickname             = s.Nickname,
        BusinessRuleSetXML   = s.BusinessRuleSetXml,
        CreatedTimestamp     = s.CreatedDate,
        CreatedBy            = s.CreatedBy,
        LastUpdatedTimestamp = s.LastModifiedDate,
        LastModifiedBy       = s.LastModifiedBy,

        BusinessRules =
            ctx.Set<AmBusinessRule>()
               .Where(r => r.BusinessRuleSetGuid == s.BusinessRuleSetGuid)
               .Select(r => new BusinessRule
               {
                   Id                      = r.BusinessRuleGuid,
                   Code                    = r.Code,
                   Name                    = r.Name,
                   Formula                 = r.Formula,
                   ErrorMessage            = r.ErrorMessage,
                   Severity                = r.Severity,
                   BusinessRuleSurrogateId = r.BusinessRuleSurrogateGuid,

                   // Fund group overrides (guard for NULL surrogate)
                   FundGroupsOverride =
                       (from o  in ctx.Set<AmBusinessRuleFoundGroupOverride>()
                        join fg in ctx.Set<AmRefFundGroup>()
                          on o.FundGroupShortName equals fg.FundGroupShortName
                        where r.BusinessRuleSurrogateGuid.HasValue
                           && o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid
                        select new FundGroup
                        {
                            ShortName = fg.FundGroupShortName,
                            LongName  = fg.LongName
                        })
                       .ToList(),

                   // Model types (explicit joins; no navs required)
                   ModelTypes =
                       (from o  in ctx.Set<AmBusinessRuleModelType>()
                        join mt in ctx.Set<AmRefAssetModelType>()
                          on o.AssetModelTypeCode equals mt.AssetModelTypeCode
                        join ut in ctx.Set<AmRefUploadType>()
                          on mt.UploadTypeCode equals ut.UploadTypeCode
                        join uc in ctx.Set<AmRefUploadCategory>()
                          on ut.UploadCategoryCode equals uc.UploadCategoryCode
                        where r.BusinessRuleSurrogateGuid.HasValue
                           && o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid
                        select new AssetModelType
                        {
                            Code           = mt.AssetModelTypeCode,
                            Description    = mt.AssetModelTypeDesc,
                            UploadTypeCode = mt.UploadTypeCode,
                            ModelUploadType = new ModelUploadType
                            {
                                Code                 = ut.UploadTypeCode,
                                Description          = ut.UploadTypeDesc,
                                DisplayOrder         = ut.DisplayOrder,
                                FinalizationRequired = ut.FinalizationRequired,
                                UploadCategory = new ModelUploadCategory
                                {
                                    Code            = uc.UploadCategoryCode,
                                    Description     = uc.UploadCategoryDesc,
                                    ContentBaseType = uc.ContentBaseType
                                }
                            }
                        })
                       .ToList()
               })
               .ToList()
    });
}

public async Task<BusinessRuleSet?> GetBusinessRuleSetByGuid(Guid guid)
{
    return await securedEntityManger.QuerySingleAsync(
        ctx => BusinessRuleSetByGuidQuery(ctx, guid),
        approvedOnly: true
    );
}


var set = await securedEntityManger.QuerySingleAsync(ctx =>
    ctx.Set<AmBusinessRuleSet>()
        .Where(s => s.BusinessRuleSetGuid == guid)
        .Select(s => new BusinessRuleSet
        {
            Id                   = s.BusinessRuleSetGuid,
            TemplateGUID         = s.TemplateGuid,
            Active               = s.Active,
            Final                = s.Final,
            Nickname             = s.Nickname,
            BusinessRuleSetXML   = s.BusinessRuleSetXml,
            CreatedTimestamp     = s.CreatedDate,
            CreatedBy            = s.CreatedBy,
            LastUpdatedTimestamp = s.LastModifiedDate,
            LastModifiedBy       = s.LastModifiedBy,

            BusinessRules = ctx.Set<AmBusinessRule>()
                .Where(r => r.BusinessRuleSetGuid == s.BusinessRuleSetGuid)
                .Select(r => new BusinessRule
                {
                    Code                    = r.Code,
                    Name                    = r.Name,
                    Formula                 = r.Formula,
                    ErrorMessage            = r.ErrorMessage,
                    Severity                = r.Severity,
                    BusinessRuleSurrogateId = r.BusinessRuleSurrogateGuid,

                    FundGroupsOverride =
                        (from o in ctx.Set<AmBusinessRuleFoundGroupOverride>()
                         join fg in ctx.Set<AmRefFundGroup>()
                            on o.FundGroupShortName equals fg.FundGroupShortName
                         where r.BusinessRuleSurrogateGuid.HasValue &&
                               o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid
                         select new FundGroup
                         {
                             ShortName = fg.FundGroupShortName,
                             LongName  = fg.LongName
                         }).ToList(),

                    ModelTypes =
                        (from o in ctx.Set<AmBusinessRuleModelType>()
                         join mt in ctx.Set<AmRefAssetModelType>()
                            on o.AssetModelTypeCode equals mt.AssetModelTypeCode
                         join ut in ctx.Set<AmRefUploadType>()
                            on mt.UploadTypeCode equals ut.UploadTypeCode
                         join uc in ctx.Set<AmRefUploadCategory>()
                            on ut.UploadCategoryCode equals uc.UploadCategoryCode
                         where r.BusinessRuleSurrogateGuid.HasValue &&
                               o.BusinessRuleSurrogateGuid == r.BusinessRuleSurrogateGuid
                         select new AssetModelType
                         {
                             Code           = mt.AssetModelTypeCode,
                             Description    = mt.AssetModelTypeDesc,
                             UploadTypeCode = mt.UploadTypeCode,
                             ModelUploadType = new ModelUploadType
                             {
                                 Code                 = ut.UploadTypeCode,
                                 Description          = ut.UploadTypeDesc,
                                 DisplayOrder         = ut.DisplayOrder,
                                 FinalizationRequired = ut.FinalizationRequired,
                                 UploadCategory = new ModelUploadCategory
                                 {
                                     Code            = uc.UploadCategoryCode,
                                     Description     = uc.UploadCategoryDesc,
                                     ContentBaseType = uc.ContentBaseType
                                 }
                             }
                         }).ToList()
                }).ToList()
        }),
    approvedOnly: true
);


var sets = await securedEntityManger.QueryListAsync(ctx =>
    ctx.Set<AmBusinessRuleSet>()
       .Where(s => s.Active)
       .Select(s => new BusinessRuleSet
       {
           Id = s.BusinessRuleSetGuid,
           Nickname = s.Nickname,
           // … same mapping logic …
       })
);
