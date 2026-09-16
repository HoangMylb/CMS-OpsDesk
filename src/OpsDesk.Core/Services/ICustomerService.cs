using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public record CustomerListItem(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    int OpenTicketsCount,
    int TotalTicketsCount,
    DateTime CreatedAt
);

public record CustomerTicketSummary(
    int Id,
    string TicketCode,
    string Subject,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime DueAt,
    bool IsOverdue
);

public record CustomerDetailDto(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CustomerTicketSummary> Tickets
);

public record CreateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? Company
);

public record UpdateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? Company
);

public interface ICustomerService
{
    Task<(List<CustomerListItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page = 1,
        int pageSize = 20);

    Task<CustomerDetailDto?> GetDetailAsync(int id);
    Task<Customer?> GetByIdAsync(int id);
    Task<ServiceResult<int>> CreateAsync(CreateCustomerRequest request, string? currentUserId = null);
    Task<ServiceResult> UpdateAsync(int id, UpdateCustomerRequest request, string? currentUserId = null);
    Task<bool> ExistsEmailAsync(string email, int? excludeId = null);
}
