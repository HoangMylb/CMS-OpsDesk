using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public RoleService(RoleManager<IdentityRole> roleManager, IAuditService auditService)
    {
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<List<RoleWithPermissions>> GetAllWithPermissionsAsync()
    {
        var result = new List<RoleWithPermissions>();
        var roles = await _roleManager.Roles.ToListAsync();

        foreach (var role in roles.OrderBy(r => r.Name))
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            result.Add(new RoleWithPermissions(
                role.Id,
                role.Name ?? string.Empty,
                claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()));
        }

        return result;
    }

    public async Task<RoleWithPermissions?> GetByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return null;

        var claims = await _roleManager.GetClaimsAsync(role);
        return new RoleWithPermissions(
            role.Id,
            role.Name ?? string.Empty,
            claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList());
    }

    public async Task<ServiceResult> CreateAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return ServiceResult.Failure($"Vai trò '{roleName}' đã tồn tại.");

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            return ServiceResult.Failure(result.Errors.Select(e => e.Description));

        await _auditService.LogAsync(null, "RoleCreated", "IdentityRole", roleName,
            newValues: new { Name = roleName });

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdatePermissionsAsync(string roleId, IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return ServiceResult.Failure("Không tìm thấy vai trò.");

        // Xóa toàn bộ permission claims cũ, rồi gán lại
        // Đây là cách đơn giản và an toàn nhất: replace toàn bộ thay vì diff
        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing.Where(c => c.Type == "Permission"))
            await _roleManager.RemoveClaimAsync(role, claim);

        var permList = permissions.ToList();
        foreach (var permission in permList)
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));

        await _auditService.LogAsync(null, "RolePermissionsUpdated", "IdentityRole", roleId,
            newValues: new { role.Name, Permissions = permList });

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null) return ServiceResult.Failure("Không tìm thấy vai trò.");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return ServiceResult.Failure(result.Errors.Select(e => e.Description));

        return ServiceResult.Success();
    }
}
