using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>Supplies savings and liquidity analysis and optional AI interpretation.</summary>
/// <param name="Analysis">Calculated savings and liquidity analysis.</param>
/// <param name="AiAvailable">Whether the configured language-model provider is available.</param>
/// <param name="AiAnalysis">Optional AI interpretation.</param>
/// <param name="AiError">Optional safe AI error message.</param>
public sealed record SavingsLiquidityAnalysisViewModel(
    SavingsLiquidityAnalysisDto Analysis,
    bool AiAvailable,
    string? AiAnalysis = null,
    string? AiError = null);

/// <summary>Supplies investment portfolio analysis and optional AI interpretation.</summary>
/// <param name="Analysis">Calculated financial-assets analysis.</param>
/// <param name="AiAvailable">Whether the configured language-model provider is available.</param>
/// <param name="AiAnalysis">Optional AI interpretation.</param>
/// <param name="AiError">Optional safe AI error message.</param>
public sealed record InvestmentPortfolioAnalysisViewModel(
    InvestmentPortfolioAnalysisDto Analysis,
    bool AiAvailable,
    string? AiAnalysis = null,
    string? AiError = null);

/// <summary>Supplies financial commitments analysis and optional AI interpretation.</summary>
/// <param name="Analysis">Calculated commitments analysis.</param>
/// <param name="AiAvailable">Whether the configured language-model provider is available.</param>
/// <param name="AiAnalysis">Optional AI interpretation.</param>
/// <param name="AiError">Optional safe AI error message.</param>
public sealed record FinancialCommitmentsAnalysisViewModel(
    FinancialCommitmentsAnalysisDto Analysis,
    bool AiAvailable,
    string? AiAnalysis = null,
    string? AiError = null);
