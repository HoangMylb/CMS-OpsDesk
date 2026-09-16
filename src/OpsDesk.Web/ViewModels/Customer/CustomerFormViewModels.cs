using System.ComponentModel.DataAnnotations;

namespace OpsDesk.Web.ViewModels.Customer;

public class CreateCustomerViewModel
{
    [Required(ErrorMessage = "Customer name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    [Display(Name = "Customer Name / Representative")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(256)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [Display(Name = "Company / Organization")]
    public string? Company { get; set; }
}

public class EditCustomerViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    [Display(Name = "Customer Name / Representative")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(256)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [Display(Name = "Company / Organization")]
    public string? Company { get; set; }
}
