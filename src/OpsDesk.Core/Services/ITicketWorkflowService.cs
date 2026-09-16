using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public record StatusTransitionRequest(
    int TicketId,
    TicketStatus TargetStatus,
    string? Notes
);

public interface ITicketWorkflowService
{
    /// <summary>
    /// Checks whether the state transition is valid according to the finite state machine matrix.
    /// </summary>
    bool CanTransition(TicketStatus currentStatus, TicketStatus targetStatus);

    /// <summary>
    /// Gets the list of valid target statuses that the ticket can transition to.
    /// </summary>
    List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus);

    /// <summary>
    /// Executes ticket status transition, updates timestamps (ResolvedAt/ClosedAt),
    /// records TicketStatusHistory, and emits AuditLog within a short Unit of Work transaction.
    /// </summary>
    Task<ServiceResult> TransitionAsync(StatusTransitionRequest request, string currentUserId);
}
