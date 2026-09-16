using System.ComponentModel.DataAnnotations;

namespace OpsDesk.Web.ViewModels.Account;

/// <summary>
/// ViewModel for login form.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email or Username is required.")]
    [Display(Name = "Email or Username")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }
}
