using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Entities;
using OpsDesk.Web.ViewModels.Account;

namespace OpsDesk.Web.Controllers;

/// <summary>
/// Xử lý toàn bộ luồng xác thực: đăng nhập, đăng xuất, đổi mật khẩu, trang từ chối truy cập.
///
/// Controller này có một ngoại lệ: các action Login và AccessDenied phải cho phép truy cập
/// khi CHƯA đăng nhập ([AllowAnonymous]). Tất cả các controller khác đều yêu cầu đăng nhập
/// (được cấu hình trong Program.cs qua AuthorizeFilter toàn cục).
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    // ================================================================
    // LOGIN
    // ================================================================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? message = null)
    {
        // Nếu đã đăng nhập rồi thì về Dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        // Hiển thị thông báo (ví dụ: "Tài khoản bị vô hiệu hóa") từ ActiveUserFilter
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

        // Tìm user theo email hoặc tên đăng nhập
        var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);

        if (user == null)
        {
            // QUAN TRỌNG VỀ BẢO MẬT: Không nói "email không tồn tại" để tránh
            // attacker dò tìm email hợp lệ trong hệ thống. Luôn dùng thông báo chung.
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        // Kiểm tra IsActive TRƯỚC khi thử đăng nhập
        // Lý do: PasswordSignInAsync vẫn thành công nếu mật khẩu đúng dù IsActive = false.
        // Ta phải chặn từ trước, không để lộ rằng mật khẩu đúng.
        if (!user.IsActive)
        {
            _logger.LogWarning("Người dùng {Email} cố đăng nhập khi tài khoản đã bị vô hiệu hóa.", model.Email);
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Admin.");
            return View(model);
        }

        // Thực hiện đăng nhập
        // lockoutOnFailure: true → sau 5 lần sai sẽ khóa tài khoản 15 phút (cấu hình trong Program.cs)
        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Người dùng {Email} đăng nhập thành công.", model.Email);

            // Redirect an toàn: chỉ cho phép redirect về URL nội bộ
            // Tránh Open Redirect attack (attacker có thể gán returnUrl = http://malicious-site.com)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Tài khoản {Email} bị khóa do đăng nhập sai nhiều lần.", model.Email);
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa tạm thời do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
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
        _logger.LogInformation("Người dùng {Email} đã đăng xuất.", email);
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

        // Identity tự kiểm tra CurrentPassword có đúng không
        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (result.Succeeded)
        {
            // Refresh sign-in: cập nhật security stamp trong cookie
            // Không làm bước này, cookie cũ có thể bị invalidate bởi Identity's security stamp validation
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Người dùng {Email} đổi mật khẩu thành công.", user.Email);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }

        // Identity trả về lỗi chi tiết (mật khẩu cũ sai, mật khẩu mới không đủ mạnh...)
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
