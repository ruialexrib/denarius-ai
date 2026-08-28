using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Infrastructure.Identity;

/// <summary>
/// Represents an application user with extended profile and consent tracking properties.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user acknowledged the demonstration data notice.
    /// </summary>
    public DateTimeOffset? DemonstrationDataAcknowledgedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user accepted the cookie consent.
    /// </summary>
    public DateTimeOffset? CookieConsentAcceptedAt { get; set; }

    /// <summary>Gets or sets whether the persistent asset balance summary is visible.</summary>
    public bool ShowAssetBalancesWidget { get; set; } = true;

    /// <summary>Gets or sets the Base64-encoded profile image synchronized from Google.</summary>
    public string? ProfileImageBase64 { get; set; }

    /// <summary>Gets or sets the media type of the synchronized profile image.</summary>
    [MaxLength(100)]
    public string? ProfileImageContentType { get; set; }
}
