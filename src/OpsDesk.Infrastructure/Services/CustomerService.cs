using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ApplicationDbContext db,
        IAuditService auditService,
        ILogger<CustomerService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(List<CustomerListItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.Email.Contains(term) ||
                (c.Company != null && c.Company.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItem(
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Company,
                c.Tickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
                c.Tickets.Count,
                c.CreatedAt))
            .ToListAsync();

        return (items, total);
    }

    public async Task<CustomerDetailDto?> GetDetailAsync(int id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Tickets)
                .ThenInclude(t => t.AssignedTo)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return null;

        var now = DateTime.UtcNow;
        var ticketDtos = customer.Tickets
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CustomerTicketSummary(
                t.Id,
                t.TicketCode,
                t.Subject,
                t.Priority,
                t.Status,
                t.AssignedTo != null ? t.AssignedTo.FullName : null,
                t.CreatedAt,
                t.DueAt,
                now > t.DueAt && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed
            ))
            .ToList();

        return new CustomerDetailDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Company,
            customer.CreatedAt,
            customer.UpdatedAt,
            ticketDtos
        );
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers.FindAsync(id);
    }

    public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null)
    {
        var normalized = email.Trim().ToLower();
        return await _db.Customers
            .AnyAsync(c => c.Email.ToLower() == normalized && (!excludeId.HasValue || c.Id != excludeId.Value));
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateCustomerRequest request, string? currentUserId = null)
    {
        var email = request.Email.Trim();
        if (await ExistsEmailAsync(email))
        {
            return ServiceResult<int>.Failure($"Email '{email}' đã được sử dụng bởi khách hàng khác.");
        }

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "CustomerCreated",
            "Customer",
            customer.Id.ToString(),
            newValues: new { customer.Name, customer.Email, customer.Company, customer.Phone });

        _logger.LogInformation("Customer '{Email}' (Id: {Id}) created by {UserId}", customer.Email, customer.Id, currentUserId);
        return ServiceResult<int>.Success(customer.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateCustomerRequest request, string? currentUserId = null)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null)
            return ServiceResult.Failure("Không tìm thấy khách hàng.");

        var email = request.Email.Trim();
        if (await ExistsEmailAsync(email, excludeId: id))
        {
            return ServiceResult.Failure($"Email '{email}' đã được sử dụng bởi khách hàng khác.");
        }

        var oldValues = new { customer.Name, customer.Email, customer.Company, customer.Phone };

        customer.Name = request.Name.Trim();
        customer.Email = email;
        customer.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        customer.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "CustomerUpdated",
            "Customer",
            customer.Id.ToString(),
            oldValues: oldValues,
            newValues: new { customer.Name, customer.Email, customer.Company, customer.Phone });

        return ServiceResult.Success();
    }
}
