namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the IntelligentReportViewModels type.
/// </summary>
public sealed record IntelligentReportViewModel(DateOnly From, DateOnly To, string GeneratedAt, string Model, string Markdown);
