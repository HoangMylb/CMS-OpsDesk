using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Web.Infrastructure;
using OpsDesk.Web.ViewModels.Ticket;

namespace OpsDesk.Web.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;

    public TicketController(
        ITicketService ticketService,
        ICurrentUserService currentUser,
        IAuthorizationService authService)
    {
        _ticketService = ticketService;
        _currentUser = currentUser;
        _authService = authService;
    }

    // GET: /Ticket
    public async Task<IActionResult> Index(
        string? search,
        TicketStatus? status,
        TicketPriority? priority,
        string? assignedToUserId,
        bool? overdueOnly,
        int page = 1)
    {
        var canViewAll = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAll)).Succeeded;
        var canViewAssigned = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAssigned)).Succeeded;

        // Nếu không có cả 2 quyền thì từ chối truy cập
        if (!canViewAll && !canViewAssigned)
            return Forbid();

        const int pageSize = 15;
        var filter = new TicketFilterParams
        {
            Search = search,
            Status = status,
            Priority = priority,
            AssignedToUserId = assignedToUserId,
            OverdueOnly = overdueOnly,
            Page = page,
            PageSize = pageSize
        };

        var (items, total) = await _ticketService.GetPagedAsync(filter, _currentUser.UserId!, canViewAll);

        var agents = await _ticketService.GetActiveAgentsForAssignmentAsync();
        var agentOptions = agents.Select(a => new SelectListItem(a.FullName, a.Id, a.Id == assignedToUserId)).ToList();
        agentOptions.Insert(0, new SelectListItem("-- Tất cả nhân viên --", ""));

        var canCreate = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Create)).Succeeded;
        var canAssign = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Assign)).Succeeded;

        var vm = new TicketListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            Priority = priority,
            AssignedToUserId = assignedToUserId,
            OverdueOnly = overdueOnly,
            AgentOptions = agentOptions,
            CanViewAll = canViewAll,
            CanCreate = canCreate,
            CanAssign = canAssign
        };

        return View(vm);
    }

    // GET: /Ticket/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var canViewAll = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAll)).Succeeded;
        var canViewAssigned = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAssigned)).Succeeded;

        if (!canViewAll && !canViewAssigned)
            return Forbid();

        var ticket = await _ticketService.GetDetailAsync(id, _currentUser.UserId!, canViewAll);
        if (ticket is null)
        {
            // Kiểm tra xem ticket có tồn tại không để trả về NotFound hay Forbid
            return NotFound();
        }

        var canAssign = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Assign)).Succeeded;
        var canUpdate = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Update)).Succeeded;
        var canResolve = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Resolve)).Succeeded;
        var canClose = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Close)).Succeeded;
        var canReopen = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Reopen)).Succeeded;

        var agents = await _ticketService.GetActiveAgentsForAssignmentAsync();
        var agentOptions = agents.Select(a => new SelectListItem(a.FullName, a.Id, a.Id == ticket.AssignedToUserId)).ToList();
        agentOptions.Insert(0, new SelectListItem("-- Chưa phân công --", ""));

        var vm = new TicketDetailViewModel
        {
            Ticket = ticket,
            SelectedAssigneeId = ticket.AssignedToUserId,
            ActiveAgentOptions = agentOptions,
            CanAssign = canAssign,
            CanUpdate = canUpdate,
            CanResolve = canResolve,
            CanClose = canClose,
            CanReopen = canReopen
        };

        return View(vm);
    }

    // GET: /Ticket/Create
    [Authorize(Policy = Permissions.Ticket.Create)]
    public async Task<IActionResult> Create(int? customerId = null)
    {
        var vm = new CreateTicketViewModel();
        if (customerId.HasValue)
        {
            vm.CustomerId = customerId.Value;
        }

        await PopulateCustomerOptionsAsync(vm);
        return View(vm);
    }

    // POST: /Ticket/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Ticket.Create)]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCustomerOptionsAsync(model);
            return View(model);
        }

        var result = await _ticketService.CreateAsync(new CreateTicketRequest(
            model.CustomerId,
            model.Subject,
            model.Description,
            model.Priority
        ), _currentUser.UserId!);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err);
            await PopulateCustomerOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Tạo phiếu hỗ trợ thành công!";
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    // POST: /Ticket/Assign
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Ticket.Assign)]
    public async Task<IActionResult> Assign(AssignTicketViewModel model)
    {
        var assigneeId = string.IsNullOrWhiteSpace(model.AssigneeId) ? null : model.AssigneeId;
        var result = await _ticketService.AssignTicketAsync(model.TicketId, assigneeId, _currentUser.UserId!);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
        }
        else
        {
            TempData["SuccessMessage"] = "Cập nhật phân công nhân viên thành công.";
        }

        return RedirectToAction(nameof(Detail), new { id = model.TicketId });
    }

    private async Task PopulateCustomerOptionsAsync(CreateTicketViewModel vm)
    {
        var customers = await _ticketService.GetCustomersForSelectAsync();
        vm.CustomerOptions = customers.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.Name} ({c.Email})" + (!string.IsNullOrEmpty(c.Company) ? $" - {c.Company}" : ""),
            Selected = c.Id == vm.CustomerId
        }).ToList();

        vm.CustomerOptions.Insert(0, new SelectListItem("-- Chọn khách hàng --", ""));
    }
}
