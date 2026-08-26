namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for InformationViewModels.
/// </summary>
public sealed record ReleaseNoteViewModel(string Version, string PublishedAt, string Url, IReadOnlyList<string> Changes);
public sealed record WhatsNewViewModel(string Version, string Framework, string OperatingSystem, bool RunningInContainer,
    string Database, string WebContainer, string McpContainer, string RepositoryUrl, IReadOnlyList<ReleaseNoteViewModel> Releases,
    string? LatestVersion, string? LatestReleaseUrl, bool UpdateAvailable);
