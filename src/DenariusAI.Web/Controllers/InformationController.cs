using System.Runtime.InteropServices;
using System.Text.Json;
using DenariusAI.Infrastructure.Identity;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Serves help, guidance, and informational content pages in the web UI.
/// </summary>
[Authorize]
public sealed class InformationController(ApplicationInfo appInfo, IHttpClientFactory httpClientFactory, IMemoryCache cache) : Controller
{
    private const string RepositoryUrl = "https://github.com/ruialexrib/denarius-ai";

    /// <summary>
    /// Displays the Help Center with the documentation topics available to the current user.
    /// </summary>
    /// <returns>The help index view.</returns>
    [HttpGet]
    public IActionResult Help()
    {
        var isAdministrator = User.IsInRole(ApplicationRoles.Administrator);
        return View(new HelpIndexViewModel(HelpCatalog.VisiblePages(isAdministrator)));
    }

    /// <summary>
    /// Displays detailed functional documentation for a specific Help Center topic.
    /// </summary>
    /// <param name="id">The unique identifier of the help topic.</param>
    /// <returns>The help detail view, a forbidden result for restricted topics, or a not found result.</returns>
    [HttpGet]
    public IActionResult HelpDetail(string id)
    {
        if (!HelpCatalog.Pages.TryGetValue(id ?? string.Empty, out var page)) return NotFound();
        if (page.AdministratorOnly && !User.IsInRole(ApplicationRoles.Administrator)) return Forbid();
        return View(page);
    }

    /// <summary>
    /// Displays the "What's New" page showing recent releases and version information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The What's New view with release notes and system information.</returns>
    [HttpGet]
    public async Task<IActionResult> WhatsNew(CancellationToken cancellationToken)
    {
        var releases = await GetReleasesAsync(cancellationToken);
        if (releases.Count == 0) releases = LocalReleases(appInfo.Version);
        var latest = releases.FirstOrDefault();
        var updateAvailable = latest is not null && IsNewerVersion(latest.Version, appInfo.Version);
        return View(new WhatsNewViewModel(appInfo.Version, RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription, string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase),
            "SQL Server 2022 Express · Online", "denarius-ai-web · Online", "denarius-ai-mcp · Perfil opcional", RepositoryUrl, releases,
            latest?.Version, latest?.Url, updateAvailable));
    }

    /// <summary>
    /// Retrieves the latest releases from the GitHub API, using cache when available.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of release notes, or an empty list if the request fails.</returns>
    private async Task<IReadOnlyList<ReleaseNoteViewModel>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<ReleaseNoteViewModel>>("github-latest-releases", out var cached)) return cached ?? [];
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DenariusAI/1.0");
            using var response = await client.GetAsync("https://api.github.com/repos/ruialexrib/denarius-ai/releases?per_page=3", cancellationToken);
            if (!response.IsSuccessStatusCode) return [];
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var releases = document.RootElement.EnumerateArray().Take(3).Select(release => new ReleaseNoteViewModel(
                release.GetProperty("tag_name").GetString() ?? "Versão",
                release.TryGetProperty("published_at", out var date) ? date.GetString()?[..10] ?? string.Empty : string.Empty,
                release.GetProperty("html_url").GetString() ?? RepositoryUrl,
                MarkdownPreview.NormalizeReleaseNotes(release.GetProperty("body").GetString()))).ToList();
            cache.Set("github-latest-releases", releases, TimeSpan.FromMinutes(15));
            return releases;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            return [];
        }
    }

    /// <summary>
    /// Determines whether the release version is newer than the current application version.
    /// </summary>
    /// <param name="releaseVersion">The release version string to compare.</param>
    /// <param name="currentVersion">The current version string to compare.</param>
    /// <returns><see langword="true"/> when the release version is newer; otherwise, <see langword="false"/>.</returns>
    private static bool IsNewerVersion(string releaseVersion, string currentVersion) =>
        Version.TryParse(releaseVersion.TrimStart('v', 'V').Split('-', '+')[0], out var latest)
        && Version.TryParse(currentVersion.Split('-', '+')[0], out var current)
        && latest > current;

    /// <summary>
    /// Generates a fallback list of release notes when the GitHub API is unavailable.
    /// </summary>
    /// <param name="version">The current application version.</param>
    /// <returns>A read-only list containing a single local release note.</returns>
    private static IReadOnlyList<ReleaseNoteViewModel> LocalReleases(string version) =>
    [
        new($"v{version}", DateTime.Today.ToString("yyyy-MM-dd"), RepositoryUrl,
            "## Português\n\n### Novas funcionalidades\n\n- Consulte as funcionalidades mais recentes do DenariusAI.\n\n### Correções\n\n- Melhorias de estabilidade e apresentação.\n\n## English\n\n### New features\n\n- Explore the latest DenariusAI features.\n\n### Fixes\n\n- Stability and presentation improvements.")
    ];
}
