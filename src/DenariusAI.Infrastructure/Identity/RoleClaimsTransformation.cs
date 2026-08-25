using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DenariusAI.Infrastructure.Identity;

/// <summary>
/// Transforms claims by adding role claims to the principal.
/// </summary>
/// <param name="userManager">The user manager used to retrieve user roles.</param>
public sealed class RoleClaimsTransformation(UserManager<ApplicationUser> userManager) : IClaimsTransformation
{
    /// <summary>
    /// Transforms the specified principal by adding role claims.
    /// </summary>
    /// <param name="principal">The claims principal to transform.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the transformed claims principal.</returns>
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return principal;
        var user = await userManager.GetUserAsync(principal); if (user is null) return principal;
        foreach (var role in await userManager.GetRolesAsync(user))
            if (!principal.IsInRole(role)) identity.AddClaim(new Claim(identity.RoleClaimType, role));
        return principal;
    }
}
