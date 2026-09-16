using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;
using OpsDesk.Web.ViewModels.AuditLog;

namespace OpsDesk.Web.Controllers;

[Authorize(Policy = Permissions.Audit.View)]
public class AuditLogController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // GET: /AuditLog
    public async Task<IActionResult> Index(
        string? userId,
        string? action,
        string? entityName,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1)
    {
        const int pageSize = 25;
        var filter = new AuditLogFilterParams
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var (items, total) = await _auditService.GetPagedLogsAsync(filter);

        var vm = new AuditLogListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            FromDate = fromDate,
            ToDate = toDate
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("partial"))
        {
            return PartialView("_AuditLogTable", vm);
        }

        return View(vm);
    }
}

