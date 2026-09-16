using System.ComponentModel.DataAnnotations;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Role;

public class RoleListViewModel
{
    public List<RoleWithPermissions> Roles { get; set; } = [];
}

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
    [MaxLength(100)]
    [Display(Name = "Tên vai trò")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel để Admin gán permissions cho một role.
/// PermissionGroups: nhóm permissions theo domain để hiển thị dạng checkbox group.
/// </summary>
public class EditRolePermissionsViewModel
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Danh sách permission được CHỌN (từ form checkboxes).</summary>
    public List<string> SelectedPermissions { get; set; } = [];

    /// <summary>Tất cả permissions trong hệ thống, nhóm theo domain.</summary>
    public List<PermissionGroup> AllGroups { get; set; } = [];
}

/// <summary>Nhóm permissions theo domain (Ticket, Customer...) để render dễ hơn trong View.</summary>
public class PermissionGroup
{
    public string Domain { get; set; } = string.Empty;
    public List<PermissionItem> Items { get; set; } = [];
}

public class PermissionItem
{
    public string Value { get; set; } = string.Empty;   // e.g. "Ticket.Create"
    public string Label { get; set; } = string.Empty;    // e.g. "Tạo ticket"
    public bool IsSelected { get; set; }
}
