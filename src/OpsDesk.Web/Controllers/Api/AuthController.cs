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
    /// Cấp JWT Token có chứa Role claims và Permission claims cho API Client / Mobile app.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetToken([FromBody] ApiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email và mật khẩu không được để trống." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Tài khoản của bạn đã bị vô hiệu hóa." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(new { message = "Tài khoản đã bị tạm khóa do nhập sai mật khẩu nhiều lần." });

            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Lấy tất cả permission claims từ các roles của user
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
