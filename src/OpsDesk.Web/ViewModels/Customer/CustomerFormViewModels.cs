using System.ComponentModel.DataAnnotations;

namespace OpsDesk.Web.ViewModels.Customer;

public class CreateCustomerViewModel
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [MaxLength(150, ErrorMessage = "Tên không được vượt quá 150 ký tự")]
    [Display(Name = "Tên khách hàng / Đại diện")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(256)]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [Display(Name = "Công ty / Tổ chức")]
    public string? Company { get; set; }
}

public class EditCustomerViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [MaxLength(150, ErrorMessage = "Tên không được vượt quá 150 ký tự")]
    [Display(Name = "Tên khách hàng / Đại diện")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(256)]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [Display(Name = "Công ty / Tổ chức")]
    public string? Company { get; set; }
}
