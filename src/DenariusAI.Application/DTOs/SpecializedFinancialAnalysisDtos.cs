using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.DTOs;

/// <summary>Represents one persisted stock holding used by specialised financial analysis.</summary>
public sealed record InvestmentStockFactDto(
    Guid Id,
    string Ticker,
    string Name,
    string? Exchange,
    string Currency,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    DateOnly PriceDate,
    bool WatchlistOnly,
    DateOnly? FirstHistoryDate,
    decimal? FirstHistoryPrice);

/// <summary>Represents one persisted insurance premium used by commitments analysis.</summary>
public sealed record InsurancePremiumFactDto(
    Guid Id,
    Guid PolicyId,
    decimal Amount,
    DateOnly DueDate,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool IsPaid);

/// <summary>Represents one persisted insurance policy and its premium schedule.</summary>
public sealed record InsurancePolicyFactDto(
    Guid Id,
    string Name,
    string Insurer,
    InsurancePolicyType Type,
    InsurancePaymentFrequency PaymentFrequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? RenewalDate,
    InsurancePolicyStatus Status,
    IReadOnlyList<InsurancePremiumFactDto> Premiums);

/// <summary>Represents deterministic savings and liquidity metrics for a selected period.</summary>
public sealed record SavingsLiquidityAnalysisDto(
    DateOnly From,
    DateOnly To,
    DateOnly ComparisonFrom,
    DateOnly ComparisonTo,
    decimal Savings,
    decimal? SavingsRate,
    decimal PreviousSavings,
    decimal? PreviousSavingsRate,
    decimal EurImmediateLiquidity,
    decimal EurSavingsAccounts,
    decimal SavingsCertificatesValue,
    decimal SavingsCertificatesYield,
    decimal? LargestLiquidAccountWeight,
    int NonEurLiquidAccountCount,
    IReadOnlyList<SavingsLiquidityAccountDto> LiquidAccounts,
    IReadOnlyList<SavingsLiquidityTrendDto> Trend,
    IReadOnlyList<SpecializedAnalysisFindingDto> Findings);

/// <summary>Represents one liquid account balance in savings and liquidity analysis.</summary>
public sealed record SavingsLiquidityAccountDto(
    Guid Id,
    string Name,
    string Type,
    string Currency,
    decimal Balance,
    decimal? Weight);

/// <summary>Represents one monthly savings observation.</summary>
public sealed record SavingsLiquidityTrendDto(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal? SavingsRate);

/// <summary>Represents the financial-assets analysis across stocks and Savings Certificates.</summary>
public sealed record InvestmentPortfolioAnalysisDto(
    DateOnly AsOf,
    decimal EurStockCost,
    decimal EurStockMarketValue,
    decimal EurStockGain,
    decimal? EurStockReturnPercentage,
    decimal SavingsCertificatesInvestment,
    decimal SavingsCertificatesValue,
    decimal SavingsCertificatesYield,
    decimal EurTrackedFinancialAssets,
    int OwnedStockPositions,
    int SavingsCertificateCount,
    int NonEurStockPositions,
    IReadOnlyList<InvestmentStockAnalysisDto> Stocks,
    IReadOnlyList<InvestmentCurrencySummaryDto> CurrencySummaries,
    IReadOnlyList<InvestmentCertificateAnalysisDto> Certificates,
    IReadOnlyList<SpecializedAnalysisFindingDto> Findings);

/// <summary>Represents one analysed owned stock position.</summary>
public sealed record InvestmentStockAnalysisDto(
    Guid Id,
    string Ticker,
    string Name,
    string Currency,
    decimal CostValue,
    decimal MarketValue,
    decimal Gain,
    decimal? ReturnPercentage,
    decimal? PeriodChangePercentage,
    DateOnly PriceDate,
    decimal? CurrencyWeight);

/// <summary>Represents stock portfolio totals within one trading currency.</summary>
public sealed record InvestmentCurrencySummaryDto(
    string Currency,
    decimal CostValue,
    decimal MarketValue,
    decimal Gain,
    decimal? ReturnPercentage,
    decimal? LargestPositionWeight);

/// <summary>Represents one Savings Certificate position in financial-assets analysis.</summary>
public sealed record InvestmentCertificateAnalysisDto(
    Guid Id,
    string SeriesNumber,
    string Description,
    decimal InvestmentValue,
    decimal CurrentValue,
    decimal Yield,
    decimal? ReturnPercentage,
    DateOnly NextCapitalization);

/// <summary>Represents known future insurance commitments and renewal pressure.</summary>
public sealed record FinancialCommitmentsAnalysisDto(
    DateOnly AsOf,
    DateOnly HorizonEnd,
    int ActivePolicies,
    decimal ScheduledPremiums,
    decimal OutstandingAmount,
    int OutstandingPremiums,
    int RenewalsWithin90Days,
    decimal PaidWithinHorizon,
    IReadOnlyList<CommitmentPolicyAnalysisDto> Policies,
    IReadOnlyList<CommitmentMonthAnalysisDto> Calendar,
    IReadOnlyList<CommitmentTypeAnalysisDto> Types,
    IReadOnlyList<SpecializedAnalysisFindingDto> Findings);

/// <summary>Represents one active policy and its known scheduled commitments.</summary>
public sealed record CommitmentPolicyAnalysisDto(
    Guid Id,
    string Name,
    string Insurer,
    string Type,
    string PaymentFrequency,
    DateOnly? RenewalDate,
    decimal ScheduledAmount,
    decimal OutstandingAmount,
    int OutstandingCount,
    DateOnly? NextDueDate);

/// <summary>Represents known insurance-premium commitments in one calendar month.</summary>
public sealed record CommitmentMonthAnalysisDto(
    int Year,
    int Month,
    decimal ScheduledAmount,
    decimal OutstandingAmount,
    int PremiumCount);

/// <summary>Represents commitment concentration by insurance type.</summary>
public sealed record CommitmentTypeAnalysisDto(
    string Type,
    decimal ScheduledAmount,
    decimal Weight,
    int PolicyCount);

/// <summary>Represents one prioritised deterministic finding shared by specialised analysis pages.</summary>
public sealed record SpecializedAnalysisFindingDto(
    string Tone,
    string Title,
    string Detail,
    string? TargetController = null,
    string? TargetAction = null,
    Guid? TargetId = null);
