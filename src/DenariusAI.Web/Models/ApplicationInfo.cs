namespace DenariusAI.Web.Models;

/// <summary>
/// Describes the application version and its user-facing description.
/// </summary>
/// <param name="Version">The application version.</param>
/// <param name="Description">The application description.</param>
public sealed record ApplicationInfo(string Version, string Description);
