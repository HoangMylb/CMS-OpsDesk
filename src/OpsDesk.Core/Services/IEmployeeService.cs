using Microsoft.AspNetCore.Identity;
using OpsDesk.Core.Entities;

namespace OpsDesk.Core.Services;

/// <summary>
/// DTO for employee listing projection to prevent fetching unnecessary entity fields.
/// </summary>
public record EmployeeListItem(
    string Id,
    string FullName,
    string? Email,
    string? DepartmentName,
    bool IsActive,
    string? RoleName,
    DateTime CreatedAt
);

/// <summary>
/// Request DTO for creating a new employee.
/// </summary>
public record CreateEmployeeRequest(
    string FullName,
    string Email,
    string Password,
    int? DepartmentId,
    string? RoleName
);

/// <summary>
/// Request DTO for updating an existing employee.
/// </summary>
public record UpdateEmployeeRequest(
    string FullName,
    string Email,
    int? DepartmentId,
    string? RoleName
);

public interface IEmployeeService
{
    /// <summary>Paginated list with filter criteria. Returns (items, totalCount).</summary>
    Task<(List<EmployeeListItem> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize);

    Task<ApplicationUser?> GetByIdAsync(string id);

    Task<List<Department>> GetAllDepartmentsAsync();

    Task<List<IdentityRole>> GetAllRolesAsync();

    /// <summary>Retrieves primary assigned role for a given user.</summary>
    Task<string?> GetCurrentRoleAsync(string userId);

    /// <summary>Creates a new employee account.</summary>
    Task<ServiceResult> CreateAsync(CreateEmployeeRequest request);

    /// <summary>Updates employee details (excluding password).</summary>
    Task<ServiceResult> UpdateAsync(string id, UpdateEmployeeRequest request);

    /// <summary>Deactivates an account (IsActive = false). Soft deactivation without deleting.</summary>
    Task<ServiceResult> DeactivateAsync(string id, string changedByUserId);

    /// <summary>Reactivates an account (IsActive = true).</summary>
    Task<ServiceResult> ActivateAsync(string id, string changedByUserId);
}
