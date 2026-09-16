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

        TempData["SuccessMessage"] = $"Đã tạo vai trò '{model.Name}' thành công.";
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

        TempData["SuccessMessage"] = $"Đã cập nhật quyền cho vai trò '{model.RoleName}' thành công.";
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

        // Không cho xóa 3 vai trò mặc định
        if (role.Name is "Admin" or "Manager" or "SupportAgent")
        {
            TempData["ErrorMessage"] = $"Không thể xóa vai trò mặc định '{role.Name}'.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _roleService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Đã xóa vai trò '{role.Name}'.";
        return RedirectToAction(nameof(Index));
    }

    private static EditRolePermissionsViewModel BuildEditRolePermissionsViewModel(RoleWithPermissions role)
    {
        var allPermissions = Permissions.GetAll().ToList();
        var selectedSet = new HashSet<string>(role.Permissions);

        // Gom nhóm permissions theo tiền tố domain
        var groups = allPermissions
            .GroupBy(p => p.Split('.')[0])
            .Select(g => new PermissionGroup
            {
                Domain = g.Key switch
                {
                    "Dashboard" => "Bảng điều khiển (Dashboard)",
                    "Employee" => "Nhân viên (Employee)",
                    "Role" => "Vai trò & Quyền (Role)",
                    "Customer" => "Khách hàng (Customer)",
                    "Ticket" => "Phiếu hỗ trợ (Ticket)",
                    "Audit" => "Nhật ký hệ thống (Audit)",
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
        Permissions.Dashboard.View => "Xem Dashboard và thống kê",
        Permissions.Employee.View => "Xem danh sách và chi tiết nhân viên",
        Permissions.Employee.Create => "Tạo mới tài khoản nhân viên",
        Permissions.Employee.Update => "Chỉnh sửa thông tin nhân viên",
        Permissions.Employee.Deactivate => "Kích hoạt / Vô hiệu hóa nhân viên",
        Permissions.Role.View => "Xem danh sách vai trò và quyền",
        Permissions.Role.Manage => "Tạo mới, chỉnh sửa quyền và xóa vai trò",
        Permissions.Customer.View => "Xem danh sách và chi tiết khách hàng",
        Permissions.Customer.Create => "Thêm khách hàng mới",
        Permissions.Customer.Update => "Chỉnh sửa thông tin khách hàng",
        Permissions.Ticket.ViewAll => "Xem toàn bộ tickets trong hệ thống",
        Permissions.Ticket.ViewAssigned => "Xem tickets được giao cho bản thân",
        Permissions.Ticket.Create => "Tạo mới ticket",
        Permissions.Ticket.Assign => "Phân công / Chuyển giao ticket cho agent",
        Permissions.Ticket.Update => "Cập nhật thông tin & nhận xử lý ticket",
        Permissions.Ticket.Resolve => "Đánh dấu ticket đã giải quyết (Resolved)",
        Permissions.Ticket.Close => "Đóng ticket (Closed)",
        Permissions.Ticket.Reopen => "Mở lại ticket đã đóng (Reopened)",
        Permissions.Audit.View => "Xem nhật ký kiểm tra hệ thống (Audit Log)",
        _ => permission
    };
}
