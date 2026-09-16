using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Entities;

namespace OpsDesk.Web.ViewModels.Employee;

public class CreateEmployeeViewModel
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [MaxLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải ít nhất 8 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Phòng ban")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Vai trò")]
    public string? RoleName { get; set; }

    // Danh sách dropdown — được điền từ controller
    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> RoleOptions { get; set; } = [];
}

public class EditEmployeeViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [MaxLength(200)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phòng ban")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Vai trò")]
    public string? RoleName { get; set; }

    public bool IsActive { get; set; }

    // Dropdown options
    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> RoleOptions { get; set; } = [];
}
