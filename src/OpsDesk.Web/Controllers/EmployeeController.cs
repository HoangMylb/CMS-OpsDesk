using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;
using OpsDesk.Web.Infrastructure;
using OpsDesk.Web.ViewModels.Employee;

namespace OpsDesk.Web.Controllers;

public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly ICurrentUserService _currentUser;

    public EmployeeController(IEmployeeService employeeService, ICurrentUserService currentUser)
    {
        _employeeService = employeeService;
        _currentUser = currentUser;
    }

    // ---------------------------------------------------------------
    // INDEX
    // ---------------------------------------------------------------

    [Authorize(Policy = Permissions.Employee.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool? isActive, int page = 1)
    {
        const int pageSize = 20;
        var (items, total) = await _employeeService.GetPagedAsync(search, isActive, page, pageSize);

        var vm = new EmployeeListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Search = search,
            IsActive = isActive,
        };
        return View(vm);
    }

    // ---------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------

    [Authorize(Policy = Permissions.Employee.Create)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = await BuildCreateViewModelAsync(new CreateEmployeeViewModel());
        return View(vm);
    }

    [Authorize(Policy = Permissions.Employee.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await BuildCreateViewModelAsync(model));

        var result = await _employeeService.CreateAsync(new CreateEmployeeRequest(
            model.FullName,
            model.Email,
            model.Password,
            model.DepartmentId,
            model.RoleName));

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err);
            return View(await BuildCreateViewModelAsync(model));
        }

        TempData["SuccessMessage"] = $"Employee '{model.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // EDIT
    // ---------------------------------------------------------------

    [Authorize(Policy = Permissions.Employee.Update)]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _employeeService.GetByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await GetCurrentRoleAsync(user.Id);

        var vm = new EditEmployeeViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            DepartmentId = user.DepartmentId,
            RoleName = currentRoles,
            IsActive = user.IsActive,
        };

        return View(await BuildEditViewModelAsync(vm));
    }

    [Authorize(Policy = Permissions.Employee.Update)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditEmployeeViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await BuildEditViewModelAsync(model));

        var result = await _employeeService.UpdateAsync(id, new UpdateEmployeeRequest(
            model.FullName,
            model.Email,
            model.DepartmentId,
            model.RoleName));

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err);
            return View(await BuildEditViewModelAsync(model));
        }

        TempData["SuccessMessage"] = $"Employee '{model.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // ACTIVATE / DEACTIVATE
    // ---------------------------------------------------------------

    [Authorize(Policy = Permissions.Employee.Deactivate)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _employeeService.DeactivateAsync(id, _currentUser.UserId!);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.FirstError;
        else
            TempData["SuccessMessage"] = "Account deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Employee.Deactivate)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        var result = await _employeeService.ActivateAsync(id, _currentUser.UserId!);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.FirstError;
        else
            TempData["SuccessMessage"] = "Account activated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private async Task<CreateEmployeeViewModel> BuildCreateViewModelAsync(CreateEmployeeViewModel vm)
    {
        var departments = await _employeeService.GetAllDepartmentsAsync();
        var roles = await _employeeService.GetAllRolesAsync();
        vm.DepartmentOptions = departments
            .Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();
        vm.RoleOptions = roles
            .Select(r => new SelectListItem(r.Name, r.Name)).ToList();
        return vm;
    }

    private async Task<EditEmployeeViewModel> BuildEditViewModelAsync(EditEmployeeViewModel vm)
    {
        var departments = await _employeeService.GetAllDepartmentsAsync();
        var roles = await _employeeService.GetAllRolesAsync();
        vm.DepartmentOptions = departments
            .Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();
        vm.RoleOptions = roles
            .Select(r => new SelectListItem(r.Name, r.Name)).ToList();
        return vm;
    }

    private async Task<string?> GetCurrentRoleAsync(string userId)
        => await _employeeService.GetCurrentRoleAsync(userId);
}
