using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Employee;

public class EmployeeListViewModel
{
    public List<EmployeeListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Filter params (giữ lại để form giữ giá trị sau khi submit)
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
