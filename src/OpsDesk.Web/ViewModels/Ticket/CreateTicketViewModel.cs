using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Enums;

namespace OpsDesk.Web.ViewModels.Ticket;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Please select a customer.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Ticket subject is required.")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Detailed description is required.")]
    [Display(Name = "Detailed Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a priority level.")]
    [Display(Name = "Priority (SLA)")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public List<SelectListItem> CustomerOptions { get; set; } = [];
}
