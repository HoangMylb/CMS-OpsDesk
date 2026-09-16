namespace OpsDesk.Core.Entities;

/// <summary>
/// A simple organisational grouping for employees.
/// e.g. "Customer Support", "Sales", "Operations", "Management".
/// In v1 an employee belongs to exactly one department.
/// </summary>
public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<ApplicationUser> Employees { get; set; } = [];
}
