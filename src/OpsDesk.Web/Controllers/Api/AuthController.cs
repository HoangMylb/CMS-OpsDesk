using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.Controllers.Api;

public record ApiLoginRequest(string Email, string Password);

public record ApiAuthResponse(
    string Token,
    string UserId,
    string FullName,
    string Email,
    IList<string> Roles,
    List<string> Permissions,
    int ExpiresInMinutes
);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Issues a JWT token containing Role and Permission claims for API clients / mobile applications.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetToken([FromBody] ApiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email/Username and password cannot be empty." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim()) ?? await _userManager.FindByNameAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Your account has been deactivated." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Account locked temporarily due to too many failed attempts." });

            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Fetch all permission claims from user's roles
        var permissionClaims = new List<Claim>();
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                permissionClaims.AddRange(claims.Where(c => c.Type == "Permission"));
            }
        }

        var token = _jwtService.GenerateToken(user, roles, permissionClaims);

        return Ok(new ApiAuthResponse(
            Token: token,
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email ?? string.Empty,
            Roles: roles,
            Permissions: permissionClaims.Select(c => c.Value).Distinct().ToList(),
            ExpiresInMinutes: 480
        ));
    }
}
