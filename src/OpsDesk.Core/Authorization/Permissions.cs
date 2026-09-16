namespace OpsDesk.Core.Authorization;

/// <summary>
/// Single source of truth for all permission strings in the system.
///
/// Why a static class instead of an enum?
/// Permission strings need to be used directly as ASP.NET Core policy names
/// and as Identity claim values. Enums require .ToString() calls everywhere
/// and can't be used as attribute arguments (compile-time constants required).
///
/// Why nested classes?
/// Groups permissions by domain, making them discoverable and preventing typos.
/// Usage: Permissions.Ticket.Assign   not the string "Ticket.Assign"
///
/// IMPORTANT: If you rename a permission string, you must also migrate any
/// existing RoleClaim rows in the database. Do not rename casually.
/// </summary>
public static class Permissions
{
    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class Employee
    {
        public const string View = "Employee.View";
        public const string Create = "Employee.Create";
        public const string Update = "Employee.Update";
        public const string Deactivate = "Employee.Deactivate";
    }

    public static class Role
    {
        public const string View = "Role.View";
        public const string Manage = "Role.Manage";
    }

    public static class Customer
    {
        public const string View = "Customer.View";
        public const string Create = "Customer.Create";
        public const string Update = "Customer.Update";
    }

    public static class Ticket
    {
        public const string ViewAll = "Ticket.ViewAll";
        public const string ViewAssigned = "Ticket.ViewAssigned";
        public const string Create = "Ticket.Create";
        public const string Assign = "Ticket.Assign";
        public const string Update = "Ticket.Update";
        public const string Resolve = "Ticket.Resolve";
        public const string Close = "Ticket.Close";
        public const string Reopen = "Ticket.Reopen";
    }

    public static class Audit
    {
        public const string View = "Audit.View";
    }

    /// <summary>
    /// Returns all permission constant values — used during role seeding
    /// and to render the "assign permissions" checkboxes in the UI.
    /// </summary>
    public static IEnumerable<string> GetAll()
    {
        return
        [
            Dashboard.View,
            Employee.View,
            Employee.Create,
            Employee.Update,
            Employee.Deactivate,
            Role.View,
            Role.Manage,
            Customer.View,
            Customer.Create,
            Customer.Update,
            Ticket.ViewAll,
            Ticket.ViewAssigned,
            Ticket.Create,
            Ticket.Assign,
            Ticket.Update,
            Ticket.Resolve,
            Ticket.Close,
            Ticket.Reopen,
            Audit.View,
        ];
    }
}
