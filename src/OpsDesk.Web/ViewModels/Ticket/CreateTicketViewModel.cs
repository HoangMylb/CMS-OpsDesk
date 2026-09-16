using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Enums;

namespace OpsDesk.Web.ViewModels.Ticket;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    [Display(Name = "Khách hàng")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Tiêu đề phiếu hỗ trợ là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    [Display(Name = "Tiêu đề yêu cầu")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả chi tiết là bắt buộc")]
    [Display(Name = "Mô tả chi tiết nội dung sự cố / yêu cầu")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn mức độ ưu tiên")]
    [Display(Name = "Mức độ ưu tiên (SLA)")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    // Dropdown list
    public List<SelectListItem> CustomerOptions { get; set; } = [];
}
