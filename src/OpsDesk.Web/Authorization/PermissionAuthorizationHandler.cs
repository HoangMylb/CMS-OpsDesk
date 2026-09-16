using Microsoft.AspNetCore.Authorization;

namespace OpsDesk.Web.Authorization;

/// <summary>
/// Checks whether the current user's ClaimsPrincipal contains the required permission claim.
///
/// How claims get onto the user:
/// 1. Admin assigns a permission to a Role via the Role management UI.
/// 2. That permission is stored as an IdentityRoleClaim with ClaimType="Permission".
/// 3. When the user logs in, ASP.NET Identity loads their role claims into the cookie.
/// 4. THIS handler reads those claims from context.User and checks for a match.
///
/// Why check claims here instead of hitting the database?
/// Claims are already in the cookie/identity — no DB round-trip needed on every request.
/// The trade-off: if an admin changes permissions mid-session, the user won't see the
/// change until they log out and back in. This is acceptable for an internal system.
/// </summary>
public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // "Permission" is the claim type we store in IdentityRoleClaim.
        var hasClaim = context.User.Claims
            .Any(c => c.Type == "Permission" && c.Value == requirement.Permission);

        if (hasClaim)
        {
            context.Succeed(requirement);
        }

        // If the claim is not found we do NOT call context.Fail() explicitly.
        // Returning without Succeed() is enough to deny access.
        // Calling Fail() would prevent other handlers from succeeding,
        // which matters if multiple handlers are registered for the same requirement.

        return Task.CompletedTask;
    }
}
