using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Ticket;

public class TicketDetailViewModel
{
    public TicketDetailDto Ticket { get; set; } = null!;

    // Assignment
    public string? SelectedAssigneeId { get; set; }
    public List<SelectListItem> ActiveAgentOptions { get; set; } = [];

    // Permissions
    public bool CanAssign { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanResolve { get; set; }
    public bool CanClose { get; set; }
    public bool CanReopen { get; set; }
}

public class AssignTicketViewModel
{
    public int TicketId { get; set; }
    public string? AssigneeId { get; set; }
}
