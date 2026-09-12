using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Supplies the deterministic global financial view and optional AI interpretation to the Razor page.
/// </summary>
/// <param name="Analysis">Authoritative calculated financial view.</param>
/// <param name="AiAvailable">Whether the configured language-model provider is available.</param>
/// <param name="AiAnalysis">Optional AI-generated executive interpretation.</param>
/// <param name="AiError">Optional safe user-facing AI error.</param>
public sealed record GlobalFinancialViewViewModel(
    GlobalFinancialViewDto Analysis,
    bool AiAvailable,
    string? AiAnalysis = null,
    string? AiError = null);
