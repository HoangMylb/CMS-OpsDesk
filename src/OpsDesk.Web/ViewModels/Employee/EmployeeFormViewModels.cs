using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OpsDesk.Web.ViewModels.Employee;

public class CreateEmployeeViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Role")]
    public string? RoleName { get; set; }

    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> RoleOptions { get; set; } = [];
}

public class EditEmployeeViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Role")]
    public string? RoleName { get; set; }

    public bool IsActive { get; set; }

    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> RoleOptions { get; set; } = [];
}
