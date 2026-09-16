using Microsoft.AspNetCore.Authorization;

namespace OpsDesk.Web.Authorization;

/// <summary>
/// Represents the requirement that a user must hold a specific permission claim.
/// This is one half of the policy-based authorization system.
///
/// How policy-based authorization works in ASP.NET Core:
/// 1. You define a Requirement (this class) — "what must be true?"
/// 2. You define a Handler (PermissionAuthorizationHandler) — "how do we check it?"
/// 3. The framework calls the handler for every [Authorize(Policy = "...")] check.
///
/// By implementing IAuthorizationRequirement we plug into ASP.NET Core's
/// standard authorization pipeline — no custom middleware needed.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
