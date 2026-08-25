using Microsoft.AspNetCore.Identity;

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
}
