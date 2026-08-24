using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DenariusAI.Infrastructure.Identity;

public sealed class RoleClaimsTransformation(UserManager<ApplicationUser> userManager) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return principal;
        var user = await userManager.GetUserAsync(principal); if (user is null) return principal;
        foreach (var role in await userManager.GetRolesAsync(user))
            if (!principal.IsInRole(role)) identity.AddClaim(new Claim(identity.RoleClaimType, role));
        return principal;
    }
}
