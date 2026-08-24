namespace DenariusAI.Web.ViewModels;

public sealed record IntelligentReportViewModel(DateOnly From, DateOnly To, string GeneratedAt, string Model, string Markdown);
