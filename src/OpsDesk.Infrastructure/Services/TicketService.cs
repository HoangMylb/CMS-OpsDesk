using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _db;
    private readonly ISlaService _slaService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ApplicationDbContext db,
        ISlaService slaService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketService> logger)
    {
        _db = db;
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
        var query = _db.Tickets.AsNoTracking();

        // 1. Phân quyền: Nếu không có quyền ViewAll (chỉ có ViewAssigned), chỉ xem tickets gán cho mình
        if (!canViewAll)
        {
            query = query.Where(t => t.AssignedToUserId == currentUserId);
        }
        else if (!string.IsNullOrEmpty(filter.AssignedToUserId))
        {
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);
        }

        // 2. Tìm kiếm từ khóa
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t =>
                t.TicketCode.Contains(term) ||
                t.Subject.Contains(term) ||
                t.Customer.Name.Contains(term) ||
                t.Customer.Email.Contains(term));
        }

        // 3. Lọc theo trạng thái
        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        // 4. Lọc theo độ ưu tiên
        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        // 5. Lọc ticket quá hạn SLA
        var now = DateTime.UtcNow;
        if (filter.OverdueOnly == true)
        {
            query = query.Where(t =>
                t.DueAt < now &&
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Closed);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TicketListItem(
                t.Id,
                t.TicketCode,
                t.Subject,
                t.CustomerId,
                t.Customer.Name,
                t.Priority,
                t.Status,
                t.AssignedToUserId,
                t.AssignedTo != null ? t.AssignedTo.FullName : null,
                t.CreatedAt,
                t.DueAt,
                now > t.DueAt && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed
            ))
            .ToListAsync();

        return (items, total);
    }

    public async Task<TicketDetailDto?> GetDetailAsync(
        int id,
        string currentUserId,
        bool canViewAll)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.StatusHistory)
                .ThenInclude(h => h.ChangedBy)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.ChangedBy)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.PreviousAssignee)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.NewAssignee)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return null;

        // Kiểm tra quyền truy cập theo tài nguyên (Resource-based Authorization)
        if (!canViewAll && ticket.AssignedToUserId != currentUserId)
        {
            return null; // Không có quyền xem ticket của người khác
        }

        var now = DateTime.UtcNow;
        var isOverdue = _slaService.IsOverdue(ticket.DueAt, ticket.Status);

        var statusHistory = ticket.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new TicketStatusHistoryDto(
                h.Id,
                h.ChangedBy != null ? h.ChangedBy.FullName : "Hệ thống",
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
                h.ChangedBy != null ? h.ChangedBy.FullName : "Hệ thống",
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
        var customer = await _db.Customers.FindAsync(request.CustomerId);
        if (customer is null)
            return ServiceResult<int>.Failure("Không tìm thấy khách hàng.");

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

        // Ghi nhận lịch sử trạng thái ban đầu
        ticket.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = TicketStatus.New,
            ToStatus = TicketStatus.New,
            ChangedByUserId = createdByUserId,
            ChangedAt = now,
            Notes = "Khởi tạo phiếu hỗ trợ"
        });

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

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

        _logger.LogInformation("Ticket {TicketCode} created for customer {CustomerId} by user {UserId}",
            ticketCode, request.CustomerId, createdByUserId);

        return ServiceResult<int>.Success(ticket.Id);
    }

    public async Task<ServiceResult> AssignTicketAsync(
        int ticketId,
        string? newAssigneeId,
        string changedByUserId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null)
            return ServiceResult.Failure("Không tìm thấy ticket.");

        if (string.IsNullOrWhiteSpace(newAssigneeId))
            return ServiceResult.Failure("Vui lòng chọn nhân viên phụ trách hợp lệ.");

        // Kiểm tra người nhận có hợp lệ và đang hoạt động không (BR-03, BR-08)
        var newAssignee = await _userManager.FindByIdAsync(newAssigneeId);
        if (newAssignee is null)
            return ServiceResult.Failure("Nhân viên được chọn không tồn tại.");

        if (!newAssignee.IsActive)
            return ServiceResult.Failure("Không thể phân công ticket cho nhân viên đã bị vô hiệu hóa.");

        var oldAssigneeId = ticket.AssignedToUserId;
        if (oldAssigneeId == newAssigneeId)
            return ServiceResult.Success(); // Không thay đổi

        var now = DateTime.UtcNow;

        // Ghi lại lịch sử phân công
        _db.TicketAssignmentHistories.Add(new TicketAssignmentHistory
        {
            TicketId = ticket.Id,
            PreviousAssigneeId = oldAssigneeId,
            NewAssigneeId = newAssigneeId,
            ChangedByUserId = changedByUserId,
            ChangedAt = now
        });

        ticket.AssignedToUserId = newAssigneeId;
        ticket.UpdatedAt = now;

        // Nếu ticket đang ở trạng thái New và được gán cho một agent, tự động chuyển sang Assigned
        if (ticket.Status == TicketStatus.New && !string.IsNullOrEmpty(newAssigneeId))
        {
            ticket.Status = TicketStatus.Assigned;
            _db.TicketStatusHistories.Add(new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = TicketStatus.New,
                ToStatus = TicketStatus.Assigned,
                ChangedByUserId = changedByUserId,
                ChangedAt = now,
                Notes = $"Tự động chuyển trạng thái khi phân công cho {newAssignee!.FullName}"
            });
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            changedByUserId,
            "TicketAssigned",
            "Ticket",
            ticket.Id.ToString(),
            oldValues: new { AssignedToUserId = oldAssigneeId },
            newValues: new { AssignedToUserId = newAssigneeId });

        return ServiceResult.Success();
    }

    public async Task<List<CustomerSelectDto>> GetCustomersForSelectAsync()
    {
        return await _db.Customers
            .AsNoTracking()
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

        // Lấy ticket mới nhất trong năm hiện tại để tính sequence
        var latestCode = await _db.Tickets
            .Where(t => t.TicketCode.StartsWith(prefix))
            .OrderByDescending(t => t.TicketCode)
            .Select(t => t.TicketCode)
            .FirstOrDefaultAsync();

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
