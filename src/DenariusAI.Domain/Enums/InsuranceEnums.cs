namespace DenariusAI.Domain.Enums;

/// <summary>
/// Identifies the type of risk covered by an insurance policy.
/// </summary>
public enum InsurancePolicyType
{
    /// <summary>Home insurance.</summary>
    Home = 1,
    /// <summary>Motor insurance.</summary>
    Motor = 2,
    /// <summary>Health insurance.</summary>
    Health = 3,
    /// <summary>Life insurance.</summary>
    Life = 4,
    /// <summary>Personal accident insurance.</summary>
    PersonalAccident = 5,
    /// <summary>Another insurance type.</summary>
    Other = 99
}

/// <summary>
/// Identifies the lifecycle state of an insurance policy.
/// </summary>
public enum InsurancePolicyStatus
{
    /// <summary>The policy is active.</summary>
    Active = 1,
    /// <summary>The policy is archived but retained for history.</summary>
    Archived = 2,
    /// <summary>The policy was cancelled.</summary>
    Cancelled = 3
}

/// <summary>
/// Identifies how frequently insurance premiums are normally due.
/// </summary>
public enum InsurancePaymentFrequency
{
    /// <summary>Monthly payment.</summary>
    Monthly = 1,
    /// <summary>Quarterly payment.</summary>
    Quarterly = 3,
    /// <summary>Semiannual payment.</summary>
    Semiannual = 6,
    /// <summary>Annual payment.</summary>
    Annual = 12,
    /// <summary>Irregular or manually scheduled payment.</summary>
    Other = 99
}
