namespace YourApp.Models;

public sealed class BusinessRuleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
}

/// <summary>
/// Keep this simple for now: we pass a templateCode (e.g., "SMT") to pick the correct manager.
/// In your real system you could look this up from templateGuid.
/// </summary>
public sealed class ValidateBusinessRuleRequest
{
    public string TemplateCode { get; set; } = "SMT";
    public BusinessRuleDto Rule { get; set; } = new();
}

public sealed class ValidateBusinessRuleResponse
{
    public bool Success { get; set; }
    public string Msg { get; set; } = string.Empty;
}


namespace YourApp.Services;

public interface IUploadManager
{
    IEnumerable<string> BrVariables();
}


namespace YourApp.Services;

/// <summary>
/// Minimal SMT manager that exposes the 17 variable names.
/// You can later build these from CreateBusinessRuleHandlers() to avoid duplication.
/// </summary>
public sealed class SmtUploadManager : IUploadManager
{
    public IEnumerable<string> BrVariables() => new[]
    {
        "RealizationDate",
        "BaseCaseExitDate",
        "EarlySaleExitDate",
        "PriorTotalCapitalization",
        "PriorNowBetGrossProfit",
        "PriorMonthNOI",
        "ICCapitalization",
        "PQProjectedOcc",
        "DebtYieldMaxFinalMaturityDate",
        "DSCRMaxFinalMaturityDate",
        "LatestValuationPercentComplete",
        "LatestValuationAsOfDate",
        "BaseCaseExitDateAdmin",
        "RealizationDateAdmin",
        "ApprovedIAAFEquity",
        "PQBCPeakEquityAsset",
        "PQBCPeakEqutyFund" // keep legacy spelling for now
    };
}

namespace YourApp.Services;

/// <summary>
/// Simple factory to resolve the appropriate manager by template code.
/// Extend the switch as you add more managers.
/// </summary>
public interface IUploadManagerResolver
{
    IUploadManager Resolve(string templateCode);
}

public sealed class UploadManagerResolver : IUploadManagerResolver
{
    private readonly SmtUploadManager _smt;

    public UploadManagerResolver(SmtUploadManager smt) => _smt = smt;

    public IUploadManager Resolve(string templateCode)
        => (templateCode ?? "SMT").ToUpperInvariant() switch
        {
            "SMT" => _smt,
            _     => _smt // default for now
        };
}


using System.Text.RegularExpressions;
using YourApp.Models;

namespace YourApp.Services;

public interface IBusinessRuleValidator
{
    (bool ok, string error) ValidateBrVariable(IUploadManager uploadManager, BusinessRuleDto rule);
}

public sealed class BusinessRuleValidator : IBusinessRuleValidator
{
    // Accepts {{Var}} ; case-sensitive, trims inside braces.
    private static readonly Regex BrVarRegex =
        new(@"\{\{([^}]*)\}\}", RegexOptions.Compiled);

    public (bool ok, string error) ValidateBrVariable(IUploadManager uploadManager, BusinessRuleDto rule)
    {
        if (uploadManager is null) return (false, "No upload manager.");
        if (rule is null) return (false, "No rule provided.");
        var formula = Uri.UnescapeDataString(rule.Formula ?? string.Empty);

        var valid = new HashSet<string>(uploadManager.BrVariables(), StringComparer.Ordinal); // exact
        foreach (Match m in BrVarRegex.Matches(formula))
        {
            var name = (m.Groups[1].Value ?? string.Empty).Trim();
            if (!valid.Contains(name))
                return (false, $"Invalid variable: {name}");
        }
        return (true, string.Empty);
    }
}
