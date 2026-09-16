using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Authorization;

namespace OpsDesk.Web.Controllers;

/// <summary>
/// Controller Dashboard — sẽ được hoàn thiện trong Phase 14.
/// Hiện tại chỉ cần một action Index để app khởi động được.
/// </summary>
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
