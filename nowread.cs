public class AmBusinessRuleSet
{
    public Guid BusinessRuleSetGuid { get; set; }
    public Guid TemplateGuid { get; set; }
    public bool Active { get; set; }
    public bool Final { get; set; }
    public string Nickname { get; set; }
    public string BusinessRuleSetXml { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string LastModifiedBy { get; set; }

    // 🔑 Add navigation
    public virtual ICollection<AmBusinessRule> BusinessRules { get; set; } = new List<AmBusinessRule>();
}

public class AmBusinessRule
{
    public Guid BusinessRuleGuid { get; set; }
    public Guid BusinessRuleSetGuid { get; set; }
    public Guid? BusinessRuleSurrogateGuid { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Formula { get; set; }
    public string ErrorMessage { get; set; }
    public int Severity { get; set; }

    // 🔑 Navigation to parent set
    public virtual AmBusinessRuleSet BusinessRuleSet { get; set; }

    // 🔑 Navigations for links
    public virtual ICollection<AmBusinessRuleFoundGroupOverride> FundGroupOverrides { get; set; } = new List<AmBusinessRuleFoundGroupOverride>();
    public virtual ICollection<AmBusinessRuleModelType> ModelTypeLinks { get; set; } = new List<AmBusinessRuleModelType>();
}

public static Expression<Func<AmBusinessRuleSet, Domain.BusinessRuleSet.BusinessRuleSet>> Expression =>
    src => new Domain.BusinessRuleSet.BusinessRuleSet
    {
        Id                   = src.BusinessRuleSetGuid,
        TemplateGUID         = src.TemplateGuid,
        Active               = src.Active,
        Final                = src.Final,
        Nickname             = src.Nickname,
        BusinessRuleSetXML   = src.BusinessRuleSetXml,
        LastUpdatedTimestamp = src.LastModifiedDate,
        LastModifiedBy       = src.LastModifiedBy,
        CreatedTimestamp     = src.CreatedDate,
        CreatedBy            = src.CreatedBy,

        BusinessRules = src.BusinessRules.Select(a => new Domain.BusinessRule.BusinessRule
        {
            Id                      = a.BusinessRuleGuid,
            Code                    = a.Code,
            Name                    = a.Name,
            Formula                 = a.Formula,
            ErrorMessage            = a.ErrorMessage,
            Severity                = a.Severity,
            BusinessRuleSurrogateId = a.BusinessRuleSurrogateGuid,

            FundGroupsOverride = a.FundGroupOverrides
                .Where(fg => fg.FundGroup != null)
                .Select(fg => new Domain.FundGroup
                {
                    ShortName = fg.FundGroup.FundGroupShortName,
                    LongName  = fg.FundGroup.LongName
                }).ToList(),

            ModelTypes = a.ModelTypeLinks
                .Where(mt => mt.AssetModelType != null)
                .Select(mt => new Domain.AssetModelType
                {
                    Code           = mt.AssetModelType.AssetModelTypeCode,
                    Description    = mt.AssetModelType.AssetModelTypeDesc,
                    UploadTypeCode = mt.AssetModelType.UploadTypeCode,

                    ModelUploadType = new Domain.Upload.ModelUploadType
                    {
                        Code                = mt.AssetModelType.UploadTypeCodeNavigation.UploadTypeCode,
                        Description         = mt.AssetModelType.UploadTypeCodeNavigation.UploadTypeDesc,
                        DisplayOrder        = mt.AssetModelType.UploadTypeCodeNavigation.DisplayOrder,
                        FinalizationRequired= mt.AssetModelType.UploadTypeCodeNavigation.FinalizationRequired,

                        UploadCategory = new Domain.Upload.ModelUploadCategory
                        {
                            Code            = mt.AssetModelType.UploadTypeCodeNavigation.UploadCategoryCodeNavigation.UploadCategoryCode,
                            Description     = mt.AssetModelType.UploadTypeCodeNavigation.UploadCategoryCodeNavigation.UploadCategoryDesc,
                            ContentBaseType = mt.AssetModelType.UploadTypeCodeNavigation.UploadCategoryCodeNavigation.ContentBaseType
                        }
                    }
                }).ToList()
        }).ToList()
    };
