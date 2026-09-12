using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Supplies the deterministic monthly budget analysis and optional AI interpretation.
/// </summary>
/// <param name="Analysis">Authoritative budget execution analysis.</param>
/// <param name="AiIsConfigured">Whether the selected AI provider is available.</param>
/// <param name="AiAnalysis">Optional generated interpretation.</param>
/// <param name="AiError">Optional safe AI error message.</param>
public sealed record BudgetExecutionAnalysisViewModel(
    BudgetExecutionAnalysisDto Analysis,
    bool AiIsConfigured,
    string? AiAnalysis = null,
    string? AiError = null);
