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
        var query = _db.Users.AsNoTracking()
            .Include(u => u.Department)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        var userRolesMap = await (
            from ur in _db.UserRoles.AsNoTracking()
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();

        var rolesByUser = userRolesMap
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(x => x.RoleName).Where(r => !string.IsNullOrEmpty(r)))
            );

        var items = users.Select(u => new EmployeeListItem(
            u.Id,
            u.FullName,
            u.Email,
            u.Department?.Name,
            u.IsActive,
            rolesByUser.TryGetValue(u.Id, out var rName) ? rName : null,
            u.CreatedAt)).ToList();

        return (items, totalCount);
    }

    // ----------------------------------------------------------------
    // GET BY ID
    // ----------------------------------------------------------------

    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    // ----------------------------------------------------------------
    // LOOKUPS
    // ----------------------------------------------------------------

    public async Task<List<Department>> GetAllDepartmentsAsync()
        => await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();

    public async Task<List<IdentityRole>> GetAllRolesAsync()
        => await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

    public async Task<string?> GetCurrentRoleAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    // ----------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------

    public async Task<ServiceResult> CreateAsync(CreateEmployeeRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return ServiceResult.Failure("Email is already registered by another account.");

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
            userId: null,
            action: "EmployeeCreated",
            entityName: "ApplicationUser",
            entityId: user.Id,
            newValues: new { user.Email, user.FullName, Role = request.RoleName });

        _logger.LogInformation("Created new employee: {Email}", user.Email);
        return ServiceResult.Success();
    }

    // ----------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------

    public async Task<ServiceResult> UpdateAsync(string id, UpdateEmployeeRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return ServiceResult.Failure("Employee not found.");

        var existingWithEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingWithEmail is not null && existingWithEmail.Id != id)
            return ServiceResult.Failure("Email is already registered by another account.");

        var oldValues = new { user.FullName, user.Email, user.DepartmentId };

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.DepartmentId = request.DepartmentId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Failure(result.Errors.Select(e => e.Description));

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
            return ServiceResult.Failure("Employee not found.");

        if (user.Id == changedByUserId)
            return ServiceResult.Failure("You cannot deactivate your own account.");

        if (!user.IsActive)
            return ServiceResult.Failure("Account is already deactivated.");

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
            return ServiceResult.Failure("Employee not found.");

        if (user.IsActive)
            return ServiceResult.Failure("Account is already active.");

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
