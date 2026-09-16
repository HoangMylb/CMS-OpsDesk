using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;
using OpsDesk.Web.ViewModels.Role;

namespace OpsDesk.Web.Controllers;

[Authorize(Policy = Permissions.Role.View)]
public class RoleController : Controller
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    // GET: /Role
    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetAllWithPermissionsAsync();
        return View(new RoleListViewModel { Roles = roles });
    }

    // GET: /Role/Create
    [Authorize(Policy = Permissions.Role.Manage)]
    public IActionResult Create()
    {
        return View(new CreateRoleViewModel());
    }

    // POST: /Role/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _roleService.CreateAsync(model.Name.Trim());
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Role '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Role/EditPermissions/id
    [Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> EditPermissions(string id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role is null)
            return NotFound();

        var vm = BuildEditRolePermissionsViewModel(role);
        return View(vm);
    }

    // POST: /Role/EditPermissions
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> EditPermissions(EditRolePermissionsViewModel model)
    {
        var result = await _roleService.UpdatePermissionsAsync(model.RoleId, model.SelectedPermissions ?? []);
        if (!result.Succeeded)
        {
            var role = await _roleService.GetByIdAsync(model.RoleId);
            if (role is null) return NotFound();

            var vm = BuildEditRolePermissionsViewModel(role);
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Permissions for role '{model.RoleName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Role/Delete/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role is null)
            return NotFound();

        // Prevent deleting default critical roles
        if (role.Name is "Admin" or "ADMIN" or "Manager" or "MANAGER" or "SupportAgent" or "SUPPORT_AGENT" or "SUPER_ADMIN" or "SuperAdmin")
        {
            TempData["ErrorMessage"] = $"Cannot delete built-in system role '{role.Name}'.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _roleService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static EditRolePermissionsViewModel BuildEditRolePermissionsViewModel(RoleWithPermissions role)
    {
        var allPermissions = Permissions.GetAll().ToList();
        var selectedSet = new HashSet<string>(role.Permissions);

        var groups = allPermissions
            .GroupBy(p => p.Split('.')[0])
            .Select(g => new PermissionGroup
            {
                Domain = g.Key switch
                {
                    "Dashboard" => "Dashboard",
                    "Employee" => "Employees",
                    "Role" => "Roles & Permissions",
                    "Customer" => "Customers",
                    "Ticket" => "Tickets",
                    "Audit" => "Audit Logs",
                    _ => g.Key
                },
                Items = g.Select(p => new PermissionItem
                {
                    Value = p,
                    Label = GetPermissionLabel(p),
                    IsSelected = selectedSet.Contains(p)
                }).ToList()
            })
            .ToList();

        return new EditRolePermissionsViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            SelectedPermissions = role.Permissions,
            AllGroups = groups
        };
    }

    private static string GetPermissionLabel(string permission) => permission switch
    {
        Permissions.Dashboard.View => "View dashboard & metrics",
        Permissions.Employee.View => "View employee list and details",
        Permissions.Employee.Create => "Create new employee account",
        Permissions.Employee.Update => "Edit employee details",
        Permissions.Employee.Deactivate => "Activate / Deactivate employees",
        Permissions.Role.View => "View roles and permissions",
        Permissions.Role.Manage => "Create, edit permissions, and delete roles",
        Permissions.Customer.View => "View customer list and details",
        Permissions.Customer.Create => "Create new customer",
        Permissions.Customer.Update => "Edit customer details",
        Permissions.Ticket.ViewAll => "View all tickets across system",
        Permissions.Ticket.ViewAssigned => "View tickets assigned to self",
        Permissions.Ticket.Create => "Create new ticket",
        Permissions.Ticket.Assign => "Assign / Reassign ticket to agent",
        Permissions.Ticket.Update => "Update ticket details and take ownership",
        Permissions.Ticket.Resolve => "Mark ticket as Resolved",
        Permissions.Ticket.Close => "Close ticket",
        Permissions.Ticket.Reopen => "Reopen closed ticket",
        Permissions.Audit.View => "View system audit logs",
        _ => permission
    };
}
