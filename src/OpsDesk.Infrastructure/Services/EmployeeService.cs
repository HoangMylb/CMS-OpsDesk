using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ILogger<EmployeeService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    // ----------------------------------------------------------------
    // LIST
    // ----------------------------------------------------------------

    public async Task<(List<EmployeeListItem> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize)
    {
        // Xây dựng query bằng LINQ — filter tại database, không load toàn bộ bảng vào memory
        // Dùng LEFT JOIN để lấy Department và Role trong một query duy nhất
        var query =
            from user in _db.Users
            join dept in _db.Departments
                on user.DepartmentId equals dept.Id into depts
            from dept in depts.DefaultIfEmpty()
            join userRole in _db.UserRoles
                on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in _db.Roles
                on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            select new { user, dept, role };

        // Áp dụng filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(x =>
                x.user.FullName.ToLower().Contains(search) ||
                (x.user.Email != null && x.user.Email.ToLower().Contains(search)));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.user.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.user.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeListItem(
                x.user.Id,
                x.user.FullName,
                x.user.Email,
                x.dept != null ? x.dept.Name : null,
                x.user.IsActive,
                x.role != null ? x.role.Name : null,
                x.user.CreatedAt))
            .ToListAsync();

        return (items, totalCount);
    }

    // ----------------------------------------------------------------
    // GET BY ID
    // ----------------------------------------------------------------

    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        return await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    // ----------------------------------------------------------------
    // LOOKUPS
    // ----------------------------------------------------------------

    public async Task<List<Department>> GetAllDepartmentsAsync()
        => await _db.Departments.OrderBy(d => d.Name).ToListAsync();

    public async Task<List<IdentityRole>> GetAllRolesAsync()
        => await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

    public async Task<string?> GetCurrentRoleAsync(string userId)
    {
        var roles = await _userManager.GetRolesAsync(
            (await _userManager.FindByIdAsync(userId))!);
        return roles.FirstOrDefault();
    }

    // ----------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------

    public async Task<ServiceResult> CreateAsync(CreateEmployeeRequest request)
    {
        // Kiểm tra email trùng
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return ServiceResult.Failure("Email đã được sử dụng bởi tài khoản khác.");

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            IsActive = true,
            DepartmentId = request.DepartmentId,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return ServiceResult.Failure(result.Errors.Select(e => e.Description));

        if (!string.IsNullOrEmpty(request.RoleName))
            await _userManager.AddToRoleAsync(user, request.RoleName);

        await _auditService.LogAsync(
            userId: null, // sẽ được set từ controller
            action: "EmployeeCreated",
            entityName: "ApplicationUser",
            entityId: user.Id,
            newValues: new { user.Email, user.FullName, Role = request.RoleName });

        _logger.LogInformation("Tạo nhân viên mới: {Email}", user.Email);
        return ServiceResult.Success();
    }

    // ----------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------

    public async Task<ServiceResult> UpdateAsync(string id, UpdateEmployeeRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return ServiceResult.Failure("Không tìm thấy nhân viên.");

        // Kiểm tra email trùng với nhân viên KHÁC
        var existingWithEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingWithEmail is not null && existingWithEmail.Id != id)
            return ServiceResult.Failure("Email đã được sử dụng bởi tài khoản khác.");

        var oldValues = new { user.FullName, user.Email, user.DepartmentId };

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.DepartmentId = request.DepartmentId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(result.Errors.Select(e => e.Description));

        // Cập nhật role nếu thay đổi
        if (!string.IsNullOrEmpty(request.RoleName))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(request.RoleName))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, request.RoleName);
            }
        }

        await _auditService.LogAsync(
            userId: null,
            action: "EmployeeUpdated",
            entityName: "ApplicationUser",
            entityId: id,
            oldValues: oldValues,
            newValues: new { request.FullName, request.Email, request.DepartmentId, Role = request.RoleName });

        return ServiceResult.Success();
    }

    // ----------------------------------------------------------------
    // DEACTIVATE / ACTIVATE
    // ----------------------------------------------------------------

    public async Task<ServiceResult> DeactivateAsync(string id, string changedByUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return ServiceResult.Failure("Không tìm thấy nhân viên.");

        if (user.Id == changedByUserId)
            return ServiceResult.Failure("Bạn không thể tự vô hiệu hóa tài khoản của mình.");

        if (!user.IsActive)
            return ServiceResult.Failure("Tài khoản đã ở trạng thái vô hiệu hóa.");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            userId: changedByUserId,
            action: "EmployeeDeactivated",
            entityName: "ApplicationUser",
            entityId: id,
            newValues: new { user.Email, IsActive = false });

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ActivateAsync(string id, string changedByUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return ServiceResult.Failure("Không tìm thấy nhân viên.");

        if (user.IsActive)
            return ServiceResult.Failure("Tài khoản đã đang hoạt động.");

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            userId: changedByUserId,
            action: "EmployeeActivated",
            entityName: "ApplicationUser",
            entityId: id,
            newValues: new { user.Email, IsActive = true });

        return ServiceResult.Success();
    }
}
