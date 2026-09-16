using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OpsDesk.Web.Authorization;

/// <summary>
/// Dynamically generates an authorization policy for any permission string
/// without requiring manual registration in Program.cs.
///
/// Problem it solves:
/// If we had to write AddPolicy("Ticket.Assign", ...) for every permission,
/// Program.cs would have 20+ policy registrations. Adding a permission would
/// require editing Program.cs.
///
/// Solution:
/// ASP.NET Core calls GetPolicyAsync() when it encounters a policy name it
/// doesn't know about. We intercept that call and create a policy on-the-fly
/// using PermissionRequirement. Any string that starts with a known domain
/// prefix is treated as a permission policy.
///
/// This means [Authorize(Policy = Permissions.Ticket.Assign)] "just works"
/// without any additional registration.
///
/// Interview note: IAuthorizationPolicyProvider is an extension point in
/// ASP.NET Core's authorization pipeline that few developers know about.
/// Being able to explain it shows deep understanding of the framework.
/// </summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if a static policy already exists (e.g. "RequireAuthenticatedUser").
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null) return policy;

        // Treat any other policy name as a permission string.
        // Build a policy that requires the user to be authenticated AND
        // have the PermissionRequirement satisfied by PermissionAuthorizationHandler.
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
