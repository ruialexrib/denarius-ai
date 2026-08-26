using DenariusAI.Infrastructure.Identity;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>Represents a successful application sign-in.</summary>
public sealed class LoginHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset LoggedInAt { get; set; } = DateTimeOffset.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
