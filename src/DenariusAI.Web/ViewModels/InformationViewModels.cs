namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the InformationViewModels type.
/// </summary>
public sealed record ReleaseNoteViewModel(string Version, string PublishedAt, string Url, IReadOnlyList<string> Changes);
/// <summary>
/// Represents the WhatsNewViewModel type.
/// </summary>
public sealed record WhatsNewViewModel(string Version, string Framework, string OperatingSystem, bool RunningInContainer,
    string Database, string WebContainer, string McpContainer, string RepositoryUrl, IReadOnlyList<ReleaseNoteViewModel> Releases,
    string? LatestVersion, string? LatestReleaseUrl, bool UpdateAvailable);
