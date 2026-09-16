using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlaService _slaService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IUnitOfWork unitOfWork,
        ISlaService slaService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketService> logger)
    {
        _unitOfWork = unitOfWork;
        _slaService = slaService;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(List<TicketListItem> Items, int TotalCount)> GetPagedAsync(
        TicketFilterParams filter,
        string currentUserId,
        bool canViewAll)
    {
        return await _unitOfWork.Tickets.GetPagedAsync(filter, currentUserId, canViewAll);
    }

    public async Task<TicketDetailDto?> GetDetailAsync(
        int id,
        string currentUserId,
        bool canViewAll)
    {
        var ticket = await _unitOfWork.Tickets.GetDetailByIdAsync(id);
        if (ticket is null) return null;

        // Resource-based authorization check
        if (!canViewAll && ticket.AssignedToUserId != currentUserId)
        {
            return null;
        }

        var isOverdue = _slaService.IsOverdue(ticket.DueAt, ticket.Status);

        var statusHistory = ticket.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new TicketStatusHistoryDto(
                h.Id,
                h.ChangedBy != null ? h.ChangedBy.FullName : "System",
                h.FromStatus,
                h.ToStatus,
                h.ChangedAt,
                h.Notes
            )).ToList();

        var assignmentHistory = ticket.AssignmentHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new TicketAssignmentHistoryDto(
                h.Id,
                h.PreviousAssignee != null ? h.PreviousAssignee.FullName : null,
                h.NewAssignee != null ? h.NewAssignee.FullName : null,
                h.ChangedBy != null ? h.ChangedBy.FullName : "System",
                h.ChangedAt
            )).ToList();

        return new TicketDetailDto(
            ticket.Id,
            ticket.TicketCode,
            ticket.CustomerId,
            ticket.Customer.Name,
            ticket.Customer.Email,
            ticket.Subject,
            ticket.Description,
            ticket.Priority,
            ticket.Status,
            ticket.AssignedToUserId,
            ticket.AssignedTo?.FullName,
            ticket.CreatedByUserId,
            ticket.CreatedBy.FullName,
            ticket.DueAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.RowVersion,
            isOverdue,
            statusHistory,
            assignmentHistory
        );
    }

    public async Task<ServiceResult<int>> CreateAsync(
        CreateTicketRequest request,
        string createdByUserId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
        if (customer is null)
            return ServiceResult<int>.Failure("Customer not found.");

        var now = DateTime.UtcNow;
        var dueAt = _slaService.CalculateDueAt(now, request.Priority);
        var ticketCode = await GenerateTicketCodeAsync(now.Year);

        var ticket = new Ticket
        {
            TicketCode = ticketCode,
            CustomerId = request.CustomerId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            Status = TicketStatus.New,
            CreatedByUserId = createdByUserId,
            DueAt = dueAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        ticket.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = TicketStatus.New,
            ToStatus = TicketStatus.New,
            ChangedByUserId = createdByUserId,
            ChangedAt = now,
            Notes = "Initial ticket created."
        });

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(
                createdByUserId,
                "TicketCreated",
                "Ticket",
                ticket.Id.ToString(),
                newValues: new
                {
                    ticket.TicketCode,
                    ticket.CustomerId,
                    ticket.Subject,
                    ticket.Priority,
                    ticket.DueAt
                });
        });

        _logger.LogInformation("Ticket {TicketCode} created by user {UserId}", ticketCode, createdByUserId);
        return ServiceResult<int>.Success(ticket.Id);
    }

    public async Task<ServiceResult> UpdateTicketAsync(
        UpdateTicketRequest request,
        string currentUserId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.Id);
        if (ticket is null)
            return ServiceResult.Failure("Ticket not found.");

        var oldValues = new
        {
            ticket.Subject,
            ticket.Description,
            ticket.Priority
        };

        ticket.Subject = request.Subject.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Priority = request.Priority;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Set original RowVersion for EF Core optimistic concurrency check
        _unitOfWork.Tickets.Update(ticket);

        try
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogAsync(
                    currentUserId,
                    "TicketUpdated",
                    "Ticket",
                    ticket.Id.ToString(),
                    oldValues: oldValues,
                    newValues: new
                    {
                        ticket.Subject,
                        ticket.Description,
                        ticket.Priority
                    });
            });

            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected when updating ticket {TicketId}", request.Id);
            return ServiceResult.Failure("Concurrency conflict: This ticket was modified by another user. Please reload the page to inspect the latest changes before saving.");
        }
    }

    public async Task<ServiceResult> AssignTicketAsync(
        int ticketId,
        string? newAssigneeId,
        string changedByUserId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult.Failure("Ticket not found.");

        if (string.IsNullOrWhiteSpace(newAssigneeId))
            return ServiceResult.Failure("Please select a valid employee for assignment.");

        var newAssignee = await _userManager.FindByIdAsync(newAssigneeId);
        if (newAssignee is null)
            return ServiceResult.Failure("Selected employee does not exist.");

        if (!newAssignee.IsActive)
            return ServiceResult.Failure("Cannot assign ticket to a deactivated employee.");

        var oldAssigneeId = ticket.AssignedToUserId;
        if (oldAssigneeId == newAssigneeId)
            return ServiceResult.Success();

        var now = DateTime.UtcNow;

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.AssignmentHistories.AddAsync(new TicketAssignmentHistory
            {
                TicketId = ticket.Id,
                PreviousAssigneeId = oldAssigneeId,
                NewAssigneeId = newAssigneeId,
                ChangedByUserId = changedByUserId,
                ChangedAt = now
            });

            ticket.AssignedToUserId = newAssigneeId;
            ticket.UpdatedAt = now;

            if (ticket.Status == TicketStatus.New)
            {
                ticket.Status = TicketStatus.Assigned;
                await _unitOfWork.StatusHistories.AddAsync(new TicketStatusHistory
                {
                    TicketId = ticket.Id,
                    FromStatus = TicketStatus.New,
                    ToStatus = TicketStatus.Assigned,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = now,
                    Notes = $"Automatic status transition on assignment to {newAssignee.FullName}"
                });
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(
                changedByUserId,
                "TicketAssigned",
                "Ticket",
                ticket.Id.ToString(),
                oldValues: new { AssignedToUserId = oldAssigneeId },
                newValues: new { AssignedToUserId = newAssigneeId });
        });

        return ServiceResult.Success();
    }

    public async Task<List<CustomerSelectDto>> GetCustomersForSelectAsync()
    {
        return await _unitOfWork.Customers
            .Query(asNoTracking: true)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerSelectDto(c.Id, c.Name, c.Email, c.Company))
            .ToListAsync();
    }

    public async Task<List<AgentSelectDto>> GetActiveAgentsForAssignmentAsync()
    {
        return await _userManager.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new AgentSelectDto(u.Id, u.FullName, u.Email ?? string.Empty))
            .ToListAsync();
    }

    private async Task<string> GenerateTicketCodeAsync(int year)
    {
        var prefix = $"TKT-{year}-";
        var latestCode = await _unitOfWork.Tickets.GetLatestTicketCodeByYearPrefixAsync(prefix);

        var seq = 1;
        if (latestCode != null && latestCode.Length > prefix.Length)
        {
            var numberPart = latestCode.Substring(prefix.Length);
            if (int.TryParse(numberPart, out var lastSeq))
            {
                seq = lastSeq + 1;
            }
        }

        return $"{prefix}{seq:D6}";
    }
}
