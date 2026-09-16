using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Ticket;

public class TicketListViewModel
{
    public List<TicketListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Filters
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public string? AssignedToUserId { get; set; }
    public bool? OverdueOnly { get; set; }

    // Dropdowns
    public List<SelectListItem> AgentOptions { get; set; } = [];

    // User permissions flag
    public bool CanViewAll { get; set; }
    public bool CanCreate { get; set; }
    public bool CanAssign { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
