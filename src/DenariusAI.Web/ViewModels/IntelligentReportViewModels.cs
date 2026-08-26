namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for IntelligentReportViewModels.
/// </summary>
public sealed record IntelligentReportViewModel(DateOnly From, DateOnly To, string GeneratedAt, string Model, string Markdown);
