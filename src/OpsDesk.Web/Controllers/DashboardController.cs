using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;
using OpsDesk.Web.Infrastructure;

namespace OpsDesk.Web.Controllers;

[Authorize(Policy = Permissions.Dashboard.View)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUser,
        IAuthorizationService authService)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var canViewAll = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAll)).Succeeded;
        var data = await _dashboardService.GetDashboardDataAsync(_currentUser.UserId!, isSystemWide: canViewAll);

        return View(data);
    }
}
