using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public class TicketFilterParams
{
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public string? AssignedToUserId { get; set; }
    public bool? OverdueOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record TicketListItem(
    int Id,
    string TicketCode,
    string Subject,
    int CustomerId,
    string CustomerName,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime DueAt,
    bool IsOverdue
);

public record TicketStatusHistoryDto(
    int Id,
    string ChangedByName,
    TicketStatus FromStatus,
    TicketStatus ToStatus,
    DateTime ChangedAt,
    string? Notes
);

public record TicketAssignmentHistoryDto(
    int Id,
    string? PreviousAssigneeName,
    string? NewAssigneeName,
    string ChangedByName,
    DateTime ChangedAt
);

public record TicketDetailDto(
    int Id,
    string TicketCode,
    int CustomerId,
    string CustomerName,
    string CustomerEmail,
    string Subject,
    string Description,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedToUserId,
    string? AssignedToName,
    string CreatedByUserId,
    string CreatedByName,
    DateTime DueAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion,
    bool IsOverdue,
    List<TicketStatusHistoryDto> StatusHistory,
    List<TicketAssignmentHistoryDto> AssignmentHistory
);

public record CreateTicketRequest(
    int CustomerId,
    string Subject,
    string Description,
    TicketPriority Priority
);

public record UpdateTicketRequest(
    int Id,
    string Subject,
    string Description,
    TicketPriority Priority,
    byte[] RowVersion
);

public record CustomerSelectDto(
    int Id,
    string Name,
    string Email,
    string? Company
);

public record AgentSelectDto(
    string Id,
    string FullName,
    string Email
);

public interface ITicketService
{
    Task<(List<TicketListItem> Items, int TotalCount)> GetPagedAsync(
        TicketFilterParams filter,
        string currentUserId,
        bool canViewAll);

    Task<TicketDetailDto?> GetDetailAsync(
        int id,
        string currentUserId,
        bool canViewAll);

    Task<ServiceResult<int>> CreateAsync(
        CreateTicketRequest request,
        string createdByUserId);

    Task<ServiceResult> UpdateTicketAsync(
        UpdateTicketRequest request,
        string currentUserId);

    Task<ServiceResult> AssignTicketAsync(
        int ticketId,
        string? newAssigneeId,
        string changedByUserId);

    Task<List<CustomerSelectDto>> GetCustomersForSelectAsync();
    Task<List<AgentSelectDto>> GetActiveAgentsForAssignmentAsync();
}
