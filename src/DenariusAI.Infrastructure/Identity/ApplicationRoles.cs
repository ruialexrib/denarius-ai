namespace DenariusAI.Infrastructure.Identity;

/// <summary>
/// Defines the application role names used throughout the system.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// The Administrator role with full system access and privileges.
    /// </summary>
    public const string Administrator = "Administrator";
    
    /// <summary>
    /// The User role with standard user access and privileges.
    /// </summary>
    public const string User = "User";
    
    /// <summary>
    /// Gets an array containing all available role names in the system.
    /// </summary>
    public static readonly string[] All = [Administrator, User];
}
