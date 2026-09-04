namespace DenariusAI.Web.Models;

/// <summary>Defines deployment-level settings for the public demonstration mode.</summary>
public sealed class DemoModeOptions
{
    /// <summary>Gets or sets whether this installation is explicitly running as a public demonstration.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the email address displayed for public demonstration access.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the demonstration password supplied by the deployment secret configuration.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets whether credentials are complete enough to be displayed on the login page.</summary>
    public bool HasCredentials => Enabled && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
