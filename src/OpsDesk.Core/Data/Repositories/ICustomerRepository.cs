using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Core.Data.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<(List<CustomerListItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetDetailWithTicketsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);
}
