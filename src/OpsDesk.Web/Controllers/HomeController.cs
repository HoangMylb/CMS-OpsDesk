using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Web.Models;

namespace OpsDesk.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet, HttpHead]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard");
    }

    [AllowAnonymous]
    [Route("Home/Error/{statusCode:int?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var vm = new ErrorViewModel
        {
            RequestId = requestId,
            StatusCode = statusCode
        };

        switch (statusCode)
        {
            case 404:
                vm.ErrorTitle = "404 — Page Not Found";
                vm.ErrorDescription = "The requested URL was not found on this server. It might have been deleted or temporarily moved.";
                _logger.LogInformation("404 Not Found at RequestId: {RequestId}", requestId);
                break;
            case 403:
                vm.ErrorTitle = "403 — Access Forbidden";
                vm.ErrorDescription = "You do not have the required permissions to perform this operation or view this resource.";
                _logger.LogWarning("403 Forbidden at RequestId: {RequestId}", requestId);
                break;
            default:
                vm.ErrorTitle = statusCode.HasValue ? $"{statusCode} — An Error Occurred" : "An Unexpected Error Occurred";
                vm.ErrorDescription = "A server-side error occurred while processing your request. The technical team has been notified.";
                _logger.LogError("Server error ({StatusCode}) at RequestId: {RequestId}", statusCode, requestId);
                break;
        }

        return View(vm);
    }
}
