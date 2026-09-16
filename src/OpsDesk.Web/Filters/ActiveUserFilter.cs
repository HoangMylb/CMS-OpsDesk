using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpsDesk.Core.Entities;

namespace OpsDesk.Web.Filters;

/// <summary>
/// Action filter that checks whether the currently logged-in user is still active (IsActive = true).
///
/// Problem addressed:
/// Authentication cookies may remain valid for hours after an admin has deactivated an account.
/// ASP.NET Core Identity does not automatically revoke existing cookies upon IsActive changes.
/// This filter ensures deactivated accounts cannot perform any further actions.
/// </summary>
public class ActiveUserFilter : IAsyncActionFilter
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ActiveUserFilter> _logger;

    public ActiveUserFilter(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ActiveUserFilter> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(user);

            // Case 1: User no longer found in DB
            // Case 2: IsActive = false (Account deactivated by Admin)
            if (appUser == null || !appUser.IsActive)
            {
                _logger.LogWarning(
                    "User {Email} attempted access while account is inactive (IsActive={IsActive}). Signing out.",
                    appUser?.Email ?? "unknown",
                    appUser?.IsActive);

                await _signInManager.SignOutAsync();

                context.Result = new RedirectToActionResult(
                    "Login", "Account",
                    new { message = "Your account has been deactivated. Please contact your administrator." });
                return;
            }
        }

        await next();
    }
}
