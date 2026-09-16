using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Employee;

public class EmployeeListViewModel
{
    public List<EmployeeListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Filter parameters retained across form requests
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
