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
    private readonly ITicketWorkflowService _workflowService;
    private readonly ITicketMessageService _messageService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;

    public TicketController(
        ITicketService ticketService,
        ITicketWorkflowService workflowService,
        ITicketMessageService messageService,
        ICurrentUserService currentUser,
        IAuthorizationService authService)
    {
        _ticketService = ticketService;
        _workflowService = workflowService;
        _messageService = messageService;
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
        agentOptions.Insert(0, new SelectListItem("-- All Staff --", ""));

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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("partial"))
        {
            return PartialView("_TicketTable", vm);
        }

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
            return NotFound();
        }

        var canAssign = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Assign)).Succeeded;
        var canUpdate = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Update)).Succeeded;
        var canResolve = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Resolve)).Succeeded;
        var canClose = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Close)).Succeeded;
        var canReopen = (await _authService.AuthorizeAsync(User, Permissions.Ticket.Reopen)).Succeeded;

        var agents = await _ticketService.GetActiveAgentsForAssignmentAsync();
        var agentOptions = agents.Select(a => new SelectListItem(a.FullName, a.Id, a.Id == ticket.AssignedToUserId)).ToList();
        agentOptions.Insert(0, new SelectListItem("-- Select Assignee --", ""));

        // Fetch messages
        var messages = await _messageService.GetMessagesAsync(ticket.Id, _currentUser.UserId!, canViewInternal: true);

        // Fetch allowed status transitions
        var allowedTransitions = _workflowService.GetAllowedTransitions(ticket.Status);

        var vm = new TicketDetailViewModel
        {
            Ticket = ticket,
            SelectedAssigneeId = ticket.AssignedToUserId,
            ActiveAgentOptions = agentOptions,
            Messages = messages,
            NewMessage = new AddTicketMessageViewModel { TicketId = ticket.Id },
            AllowedTransitions = allowedTransitions,
            CanAssign = canAssign,
            CanUpdate = canUpdate,
            CanResolve = canResolve,
            CanClose = canClose,
            CanReopen = canReopen,
            CanAddInternalNote = true
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

        TempData["SuccessMessage"] = "Ticket created successfully!";
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    // GET: /Ticket/Edit/5
    [Authorize(Policy = Permissions.Ticket.Update)]
    public async Task<IActionResult> Edit(int id)
    {
        var canViewAll = (await _authService.AuthorizeAsync(User, Permissions.Ticket.ViewAll)).Succeeded;
        var ticket = await _ticketService.GetDetailAsync(id, _currentUser.UserId!, canViewAll);
        if (ticket is null) return NotFound();

        var vm = new EditTicketViewModel
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            CustomerName = ticket.CustomerName,
            Subject = ticket.Subject,
            Description = ticket.Description,
            Priority = ticket.Priority,
            RowVersionBase64 = Convert.ToBase64String(ticket.RowVersion)
        };

        return View(vm);
    }

    // POST: /Ticket/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Ticket.Update)]
    public async Task<IActionResult> Edit(int id, EditTicketViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(model.RowVersionBase64);
        }
        catch
        {
            return BadRequest("Invalid concurrency token.");
        }

        var result = await _ticketService.UpdateTicketAsync(new UpdateTicketRequest(
            model.Id,
            model.Subject,
            model.Description,
            model.Priority,
            rowVersion
        ), _currentUser.UserId!);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Ticket updated successfully.";
        return RedirectToAction(nameof(Detail), new { id = model.Id });
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
            TempData["SuccessMessage"] = "Ticket assignment updated successfully.";
        }

        return RedirectToAction(nameof(Detail), new { id = model.TicketId });
    }

    // POST: /Ticket/TransitionStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransitionStatus(TransitionTicketStatusViewModel model)
    {
        var target = model.TargetStatus;
        string? requiredPolicy = target switch
        {
            TicketStatus.InProgress => Permissions.Ticket.Update,
            TicketStatus.Resolved   => Permissions.Ticket.Resolve,
            TicketStatus.Closed     => Permissions.Ticket.Close,
            TicketStatus.Reopened   => Permissions.Ticket.Reopen,
            _                       => null
        };

        if (requiredPolicy != null)
        {
            var authResult = await _authService.AuthorizeAsync(User, requiredPolicy);
            if (!authResult.Succeeded)
            {
                TempData["ErrorMessage"] = $"You do not have permission to transition ticket to '{target}'.";
                return RedirectToAction(nameof(Detail), new { id = model.TicketId });
            }
        }

        var result = await _workflowService.TransitionAsync(
            new StatusTransitionRequest(model.TicketId, model.TargetStatus, model.Notes),
            _currentUser.UserId!);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
        }
        else
        {
            TempData["SuccessMessage"] = $"Ticket status transitioned to '{target}'.";
        }

        return RedirectToAction(nameof(Detail), new { id = model.TicketId });
    }

    // POST: /Ticket/AddMessage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessage(AddTicketMessageViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Content))
        {
            TempData["ErrorMessage"] = "Message content or note cannot be empty.";
            return RedirectToAction(nameof(Detail), new { id = model.TicketId });
        }

        var result = await _messageService.AddMessageAsync(
            new AddMessageRequest(model.TicketId, model.Content, model.IsInternal),
            _currentUser.UserId!);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
        }
        else
        {
            TempData["SuccessMessage"] = model.IsInternal
                ? "Internal note added successfully."
                : "Reply posted successfully.";
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

        vm.CustomerOptions.Insert(0, new SelectListItem("-- Select Customer --", ""));
    }
}
