using Microsoft.AspNetCore.Identity;

namespace OpsDesk.Core.Services;

public record RoleWithPermissions(
    string Id,
    string Name,
    List<string> Permissions
);

public interface IRoleService
{
    Task<List<RoleWithPermissions>> GetAllWithPermissionsAsync();
    Task<RoleWithPermissions?> GetByIdAsync(string id);
    Task<ServiceResult> CreateAsync(string roleName);
    Task<ServiceResult> UpdatePermissionsAsync(string roleId, IEnumerable<string> permissions);
    Task<ServiceResult> DeleteAsync(string roleId);
}
