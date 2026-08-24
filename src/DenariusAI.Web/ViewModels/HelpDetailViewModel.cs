namespace DenariusAI.Web.ViewModels;

public sealed record HelpSectionViewModel(string Title, IReadOnlyList<string> Items);
public sealed record HelpDetailViewModel(string Id, string Title, string Subtitle, string Controller, string Action,
    string ActionLabel, IReadOnlyList<HelpSectionViewModel> Sections);
