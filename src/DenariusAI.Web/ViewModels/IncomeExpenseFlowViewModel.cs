using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Supplies the deterministic income, expense and flow analysis and its optional AI interpretation.
/// </summary>
/// <param name="Analysis">Calculated flow analysis.</param>
/// <param name="AiAvailable">Whether the configured language-model provider is available.</param>
/// <param name="AiAnalysis">Optional AI interpretation.</param>
/// <param name="AiError">Optional safe AI error message.</param>
public sealed record IncomeExpenseFlowViewModel(
    IncomeExpenseFlowAnalysisDto Analysis,
    bool AiAvailable,
    string? AiAnalysis = null,
    string? AiError = null);
