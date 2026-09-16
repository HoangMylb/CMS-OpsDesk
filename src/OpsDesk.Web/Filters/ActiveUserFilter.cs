using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpsDesk.Core.Entities;

namespace OpsDesk.Web.Filters;

/// <summary>
/// Action filter kiểm tra xem người dùng đang đăng nhập có còn IsActive = true không.
///
/// Vấn đề cần giải quyết:
/// Cookie xác thực có thể còn hiệu lực nhiều giờ sau khi Admin đã vô hiệu hóa tài khoản.
/// ASP.NET Core Identity không tự động thu hồi cookie khi IsActive thay đổi.
/// Filter này giải quyết vấn đề đó bằng cách kiểm tra database trong mỗi request.
///
/// Trade-off:
/// Mỗi request của user đã đăng nhập sẽ có một truy vấn DB nhỏ để kiểm tra IsActive.
/// Đây là chi phí chấp nhận được cho một hệ thống nội bộ — security > performance tuyệt đối.
/// Nếu performance là vấn đề, có thể cache kết quả này trong 1-2 phút.
///
/// Đăng ký filter này trong Program.cs:
///   options.Filters.Add&lt;ActiveUserFilter&gt;();
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

        // Chỉ kiểm tra với user đã đăng nhập
        if (user.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(user);

            // Trường hợp 1: Không tìm thấy user trong DB (account bị xóa — hiếm xảy ra)
            // Trường hợp 2: IsActive = false (account bị vô hiệu hóa bởi Admin)
            if (appUser == null || !appUser.IsActive)
            {
                _logger.LogWarning(
                    "Người dùng {Email} cố truy cập khi tài khoản không còn hợp lệ (IsActive={IsActive}). Đăng xuất.",
                    appUser?.Email ?? "unknown",
                    appUser?.IsActive);

                // Đăng xuất — xóa cookie
                await _signInManager.SignOutAsync();

                // Redirect về trang Login với thông báo rõ ràng
                context.Result = new RedirectToActionResult(
                    "Login", "Account",
                    new { message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Admin." });
                return;
            }
        }

        // User hợp lệ — tiếp tục xử lý request
        await next();
    }
}
