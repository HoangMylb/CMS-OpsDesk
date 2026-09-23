using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Seeding;
using OpsDesk.Web.ViewModels.Account;

namespace OpsDesk.Web.Controllers;

/// <summary>
/// Handles authentication workflow: login, logout, password change, access denied.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    // ================================================================
    // LOGIN
    // ================================================================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? message = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        if (!string.IsNullOrEmpty(message))
            ViewBag.ErrorMessage = message;

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // Find user by email or username
        var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);

        if (user == null)
        {
            // Security: generic error message to prevent account enumeration
            ModelState.AddModelError(string.Empty, "Invalid email/username or password.");
            return View(model);
        }

        // Check IsActive prior to sign-in attempt
        if (!user.IsActive)
        {
            _logger.LogWarning("User {Email} attempted login while account is deactivated.", model.Email);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account {Email} locked out due to multiple failed login attempts.", model.Email);
            ModelState.AddModelError(string.Empty, "Account locked temporarily due to too many failed attempts. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email/username or password.");
        return View(model);
    }

    // ================================================================
    // LOGOUT
    // ================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var email = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Email} logged out.", email);
        return RedirectToAction("Login", "Account");
    }

    // ================================================================
    // CHANGE PASSWORD
    // ================================================================

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        // Public accounts are deliberately resettable only by redeploying the isolated demo.
        // This prevents one reviewer from locking subsequent reviewers out of the portfolio.
        if (bool.TryParse(_configuration["DemoSeed:Enabled"], out var demoSeedEnabled) && demoSeedEnabled &&
            PortfolioDemoAccounts.IsPublicDemoEmail(user.Email))
        {
            ModelState.AddModelError(string.Empty, "Demo account passwords are managed by the isolated demo environment and cannot be changed.");
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {Email} changed password successfully.", user.Email);

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("ChangePassword");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // ================================================================
    // ACCESS DENIED
    // ================================================================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
