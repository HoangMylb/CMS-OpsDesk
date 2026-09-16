using System.ComponentModel.DataAnnotations;
using OpsDesk.Core.Services;

namespace OpsDesk.Web.ViewModels.Role;

public class RoleListViewModel
{
    public List<RoleWithPermissions> Roles { get; set; } = [];
}

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(100)]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for managing role permissions.
/// PermissionGroups: Groups permissions by domain to display as grouped checkboxes.
/// </summary>
public class EditRolePermissionsViewModel
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;

    /// <summary>List of selected permissions from form checkboxes.</summary>
    public List<string> SelectedPermissions { get; set; } = [];

    /// <summary>All permissions in the system, grouped by domain.</summary>
    public List<PermissionGroup> AllGroups { get; set; } = [];
}

/// <summary>Groups permissions by domain (Ticket, Customer...) for view rendering.</summary>
public class PermissionGroup
{
    public string Domain { get; set; } = string.Empty;
    public List<PermissionItem> Items { get; set; } = [];
}

public class PermissionItem
{
    public string Value { get; set; } = string.Empty;   // e.g. "Ticket.Create"
    public string Label { get; set; } = string.Empty;    // e.g. "Create new ticket"
    public bool IsSelected { get; set; }
}
