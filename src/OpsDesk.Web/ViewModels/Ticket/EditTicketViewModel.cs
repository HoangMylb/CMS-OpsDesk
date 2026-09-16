using System.ComponentModel.DataAnnotations;
using OpsDesk.Core.Enums;

namespace OpsDesk.Web.ViewModels.Ticket;

public class EditTicketViewModel
{
    public int Id { get; set; }

    public string TicketCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    [MaxLength(200, ErrorMessage = "Subject must not exceed 200 characters")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required")]
    [Display(Name = "Priority")]
    public TicketPriority Priority { get; set; }

    /// <summary>
    /// Base64-encoded RowVersion for EF Core optimistic concurrency checking.
    /// </summary>
    public string RowVersionBase64 { get; set; } = string.Empty;
}
