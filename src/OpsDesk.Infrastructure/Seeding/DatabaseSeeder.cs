using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Seeding;

/// <summary>
/// Khởi tạo dữ liệu mẫu an toàn cho môi trường development.
///
/// Nguyên tắc:
/// - Idempotent: chạy nhiều lần không tạo duplicate (kiểm tra trước khi insert)
/// - Không dùng migration để seed dữ liệu nghiệp vụ —
///   migration chỉ nên chứa schema, không chứa dữ liệu có thể thay đổi
/// - Mật khẩu demo chỉ dùng trong development, KHÔNG commit vào production config
///
/// Gọi trong Program.cs sau khi database đã migrate xong.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedDepartmentsAsync();
        await SeedRolesAndPermissionsAsync();
        await SeedUsersAsync();
    }

    // ================================================================
    // DEPARTMENTS
    // ================================================================

    private async Task SeedDepartmentsAsync()
    {
        var departments = new[]
        {
            new Department { Name = "Management", Description = "Ban quản lý" },
            new Department { Name = "Customer Support", Description = "Phòng hỗ trợ khách hàng" },
            new Department { Name = "Sales", Description = "Phòng kinh doanh" },
            new Department { Name = "Operations", Description = "Phòng vận hành" },
        };

        foreach (var dept in departments)
        {
            // Idempotent: chỉ thêm nếu chưa có
            if (!await _db.Departments.AnyAsync(d => d.Name == dept.Name))
            {
                _db.Departments.Add(dept);
                _logger.LogInformation("Seed department: {Name}", dept.Name);
            }
        }

        await _db.SaveChangesAsync();
    }

    // ================================================================
    // ROLES & PERMISSIONS
    // ================================================================

    private async Task SeedRolesAndPermissionsAsync()
    {
        // Định nghĩa 3 role mặc định và permissions của từng role
        var rolePermissions = new Dictionary<string, string[]>
        {
            ["Admin"] = Permissions.GetAll().ToArray(),

            ["Manager"] =
            [
                Permissions.Dashboard.View,
                Permissions.Employee.View,
                Permissions.Customer.View,
                Permissions.Customer.Create,
                Permissions.Customer.Update,
                Permissions.Ticket.ViewAll,
                Permissions.Ticket.ViewAssigned,
                Permissions.Ticket.Create,
                Permissions.Ticket.Assign,
                Permissions.Ticket.Update,
                Permissions.Ticket.Resolve,
                Permissions.Ticket.Close,
                Permissions.Ticket.Reopen,
            ],

            ["SupportAgent"] =
            [
                Permissions.Dashboard.View,
                Permissions.Customer.View,
                Permissions.Ticket.ViewAssigned,
                Permissions.Ticket.Create,
                Permissions.Ticket.Update,
                Permissions.Ticket.Resolve,
            ],
        };

        foreach (var (roleName, permissions) in rolePermissions)
        {
            // Tạo role nếu chưa có
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var createResult = await _roleManager.CreateAsync(role);

                if (!createResult.Succeeded)
                {
                    _logger.LogError("Không tạo được role {Role}: {Errors}",
                        roleName,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                _logger.LogInformation("Seed role: {Role}", roleName);
            }

            // Gán permissions dưới dạng Claims của Role
            // Đây là cơ chế chính: khi user đăng nhập, Identity load các RoleClaim này
            // vào ClaimsPrincipal → PermissionAuthorizationHandler kiểm tra claim này
            var existingRole = await _roleManager.FindByNameAsync(roleName);
            if (existingRole == null) continue;

            var existingClaims = await _roleManager.GetClaimsAsync(existingRole);

            foreach (var permission in permissions)
            {
                // Claim type = "Permission", value = tên permission cụ thể
                var alreadyExists = existingClaims
                    .Any(c => c.Type == "Permission" && c.Value == permission);

                if (!alreadyExists)
                {
                    await _roleManager.AddClaimAsync(
                        existingRole,
                        new System.Security.Claims.Claim("Permission", permission));
                }
            }
        }
    }

    // ================================================================
    // USERS (DEMO ACCOUNTS)
    // ================================================================

    private async Task SeedUsersAsync()
    {
        var managementDept = await _db.Departments.FirstOrDefaultAsync(d => d.Name == "Management");
        var supportDept = await _db.Departments.FirstOrDefaultAsync(d => d.Name == "Customer Support");

        var users = new[]
        {
            new
            {
                FullName = "Admin OpsDesk",
                Email = "admin@opsdesk.local",
                Password = "Admin@123456",
                Role = "Admin",
                Department = managementDept,
            },
            new
            {
                FullName = "Nguyễn Văn Manager",
                Email = "manager@opsdesk.local",
                Password = "Manager@123456",
                Role = "Manager",
                Department = managementDept,
            },
            new
            {
                FullName = "Trần Thị Agent",
                Email = "agent1@opsdesk.local",
                Password = "Agent@123456",
                Role = "SupportAgent",
                Department = supportDept,
            },
            new
            {
                FullName = "Lê Văn Agent",
                Email = "agent2@opsdesk.local",
                Password = "Agent@123456",
                Role = "SupportAgent",
                Department = supportDept,
            },
        };

        foreach (var userData in users)
        {
            // Kiểm tra đã tồn tại chưa
            if (await _userManager.FindByEmailAsync(userData.Email) != null)
                continue;

            var user = new ApplicationUser
            {
                FullName = userData.FullName,
                UserName = userData.Email,  // Identity dùng UserName để xác thực
                Email = userData.Email,
                EmailConfirmed = true,      // Bỏ qua bước xác nhận email trong dev
                IsActive = true,
                DepartmentId = userData.Department?.Id,
                CreatedAt = DateTime.UtcNow,
            };

            // Identity tự hash mật khẩu — không bao giờ lưu plain text
            var result = await _userManager.CreateAsync(user, userData.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, userData.Role);
                _logger.LogInformation("Seed user: {Email} / Role: {Role}", userData.Email, userData.Role);
            }
            else
            {
                _logger.LogError("Không tạo được user {Email}: {Errors}",
                    userData.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
