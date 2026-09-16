using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<CustomerService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(List<CustomerListItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        return await _unitOfWork.Customers.GetPagedAsync(search, page, pageSize);
    }

    public async Task<CustomerDetailDto?> GetDetailAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetDetailWithTicketsAsync(id);
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
                now > t.DueAt && t.Status != Core.Enums.TicketStatus.Resolved && t.Status != Core.Enums.TicketStatus.Closed
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
        return await _unitOfWork.Customers.GetByIdAsync(id);
    }

    public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null)
    {
        return await _unitOfWork.Customers.ExistsEmailAsync(email, excludeId);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateCustomerRequest request, string? currentUserId = null)
    {
        var email = request.Email.Trim();
        if (await ExistsEmailAsync(email))
        {
            return ServiceResult<int>.Failure($"Email '{email}' is already in use by another customer.");
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

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "CustomerCreated",
                "Customer",
                customer.Id.ToString(),
                newValues: new { customer.Name, customer.Email, customer.Company, customer.Phone });
        });

        _logger.LogInformation("Customer '{Email}' (Id: {Id}) created by {UserId}", customer.Email, customer.Id, currentUserId);
        return ServiceResult<int>.Success(customer.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateCustomerRequest request, string? currentUserId = null)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer is null)
            return ServiceResult.Failure("Customer not found.");

        var email = request.Email.Trim();
        if (await ExistsEmailAsync(email, excludeId: id))
        {
            return ServiceResult.Failure($"Email '{email}' is already in use by another customer.");
        }

        var oldValues = new { customer.Name, customer.Email, customer.Company, customer.Phone };

        customer.Name = request.Name.Trim();
        customer.Email = email;
        customer.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        customer.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                "CustomerUpdated",
                "Customer",
                customer.Id.ToString(),
                oldValues: oldValues,
                newValues: new { customer.Name, customer.Email, customer.Company, customer.Phone });
        });

        return ServiceResult.Success();
    }
}
