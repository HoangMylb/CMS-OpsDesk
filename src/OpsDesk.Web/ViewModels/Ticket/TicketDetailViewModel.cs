using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Ticket;

public class TicketDetailViewModel
{
    public TicketDetailDto Ticket { get; set; } = null!;

    // Assignment
    public string? SelectedAssigneeId { get; set; }
    public List<SelectListItem> ActiveAgentOptions { get; set; } = [];

    // Messages & Notes
    public List<TicketMessageDto> Messages { get; set; } = [];
    public AddTicketMessageViewModel NewMessage { get; set; } = new();

    // Workflow Transitions
    public List<TicketStatus> AllowedTransitions { get; set; } = [];

    // Permissions
    public bool CanAssign { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanResolve { get; set; }
    public bool CanClose { get; set; }
    public bool CanReopen { get; set; }
    public bool CanAddInternalNote { get; set; }
}

public class AssignTicketViewModel
{
    public int TicketId { get; set; }
    public string? AssigneeId { get; set; }
}

public class AddTicketMessageViewModel
{
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Nội dung phản hồi hoặc ghi chú không được để trống")]
    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
}

public class TransitionTicketStatusViewModel
{
    public int TicketId { get; set; }
    public TicketStatus TargetStatus { get; set; }
    public string? Notes { get; set; }
}
