using System.ComponentModel.DataAnnotations;

namespace OpsDesk.Web.ViewModels.Account;

/// <summary>
/// ViewModel cho form đăng nhập.
/// 
/// Tại sao dùng ViewModel thay vì ApplicationUser trực tiếp?
/// ApplicationUser chứa nhiều trường nhạy cảm (PasswordHash, SecurityStamp...).
/// ViewModel chỉ chứa đúng những gì form cần — không để lộ dữ liệu thừa
/// và không bao giờ bind trực tiếp model Entity từ HTTP request (tránh Mass Assignment attack).
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email hoặc Tên đăng nhập là bắt buộc")]
    [Display(Name = "Email hoặc Tên đăng nhập")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}
