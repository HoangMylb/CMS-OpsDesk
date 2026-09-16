using Microsoft.AspNetCore.Identity;
using OpsDesk.Core.Entities;

namespace OpsDesk.Core.Services;

// DTO dùng cho danh sách nhân viên (projection để tránh load toàn bộ entity)
public record EmployeeListItem(
    string Id,
    string FullName,
    string? Email,
    string? DepartmentName,
    bool IsActive,
    string? RoleName,
    DateTime CreatedAt
);

// Request object để tạo nhân viên mới — tách biệt khỏi ViewModel Web
public record CreateEmployeeRequest(
    string FullName,
    string Email,
    string Password,
    int? DepartmentId,
    string? RoleName
);

// Request object để cập nhật nhân viên
public record UpdateEmployeeRequest(
    string FullName,
    string Email,
    int? DepartmentId,
    string? RoleName
);

public interface IEmployeeService
{
    /// <summary>Danh sách phân trang với bộ lọc. Trả về (items, totalCount).</summary>
    Task<(List<EmployeeListItem> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize);

    Task<ApplicationUser?> GetByIdAsync(string id);

    Task<List<Department>> GetAllDepartmentsAsync();

    Task<List<IdentityRole>> GetAllRolesAsync();

    /// <summary>Lấy role hiện tại của một user (mỗi user có 1 role).</summary>
    Task<string?> GetCurrentRoleAsync(string userId);

    /// <summary>Tạo tài khoản nhân viên mới.</summary>
    Task<ServiceResult> CreateAsync(CreateEmployeeRequest request);

    /// <summary>Cập nhật thông tin nhân viên (không thay đổi mật khẩu).</summary>
    Task<ServiceResult> UpdateAsync(string id, UpdateEmployeeRequest request);

    /// <summary>Vô hiệu hóa tài khoản (IsActive = false). Không xóa.</summary>
    Task<ServiceResult> DeactivateAsync(string id, string changedByUserId);

    /// <summary>Kích hoạt lại tài khoản (IsActive = true).</summary>
    Task<ServiceResult> ActivateAsync(string id, string changedByUserId);
}
