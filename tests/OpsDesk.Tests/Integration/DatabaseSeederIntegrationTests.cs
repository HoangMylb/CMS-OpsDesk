using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Infrastructure.Seeding;
using Xunit;

namespace OpsDesk.Tests.Integration;

public class DatabaseSeederIntegrationTests
{
    [Fact]
    public async Task SeedAsync_WithConfiguredAdminUsers_ShouldSeedUsersAndRolesSuccessfully()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AdminUsers:0:UserName"] = "HoangMy",
            ["AdminUsers:0:Email"] = "admin@hoangmy.vn",
            ["AdminUsers:0:Password"] = "TestOnly!2026",
            ["AdminUsers:0:Roles:0"] = "ADMIN",
            ["AdminUsers:0:Roles:1"] = "SUPER_ADMIN",
            ["AdminUsers:0:Roles:2"] = "MANAGER",
            ["AdminUsers:0:Roles:3"] = "SUPPORT_AGENT",
            ["AdminUsers:1:UserName"] = "MiniAppCore",
            ["AdminUsers:1:Email"] = "admin@MiniAppCore.vn",
            ["AdminUsers:1:Password"] = "TestOnly!2026",
            ["AdminUsers:1:Roles:0"] = "ADMIN",
            ["AdminUsers:1:Roles:1"] = "SUPER_ADMIN",
            ["AdminUsers:1:Roles:2"] = "MANAGER",
            ["AdminUsers:1:Roles:3"] = "SUPPORT_AGENT",
        };

        var serviceProvider = CreateServiceProvider(inMemorySettings);

        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Act
        await seeder.SeedAsync();

        // Assert - Roles created
        Assert.True(await roleManager.RoleExistsAsync("ADMIN"));
        Assert.True(await roleManager.RoleExistsAsync("SUPER_ADMIN"));
        Assert.True(await roleManager.RoleExistsAsync("MANAGER"));
        Assert.True(await roleManager.RoleExistsAsync("SUPPORT_AGENT"));

        // Assert - HoangMy user created
        var hoangMyUser = await userManager.FindByEmailAsync("admin@hoangmy.vn");
        Assert.NotNull(hoangMyUser);
        Assert.Equal("HoangMy", hoangMyUser.UserName);
        Assert.True(hoangMyUser.IsActive);
        Assert.True(hoangMyUser.EmailConfirmed);

        var hoangMyRoles = await userManager.GetRolesAsync(hoangMyUser);
        Assert.Contains(hoangMyRoles, r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hoangMyRoles, r => r.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hoangMyRoles, r => r.Equals("MANAGER", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hoangMyRoles, r => r.Equals("SUPPORT_AGENT", StringComparison.OrdinalIgnoreCase));

        // Assert - MiniAppCore user created
        var miniAppUser = await userManager.FindByEmailAsync("admin@MiniAppCore.vn");
        Assert.NotNull(miniAppUser);
        Assert.Equal("MiniAppCore", miniAppUser.UserName);
        Assert.True(miniAppUser.IsActive);

        var miniAppRoles = await userManager.GetRolesAsync(miniAppUser);
        Assert.Contains(miniAppRoles, r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(miniAppRoles, r => r.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(miniAppRoles, r => r.Equals("MANAGER", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(miniAppRoles, r => r.Equals("SUPPORT_AGENT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeedAsync_DoesNotCreatePublicDemoUsers_WhenDemoSeedIsNotExplicitlyEnabled()
    {
        using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["DemoSeed:Password"] = "PortfolioDemo!2026",
        });
        using var scope = serviceProvider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await userManager.FindByEmailAsync(PortfolioDemoAccounts.ManagerEmail));
        Assert.Null(await userManager.FindByEmailAsync(PortfolioDemoAccounts.SupportAgentEmail));
    }

    [Fact]
    public async Task SeedAsync_CreatesPublicDemoUsersOnlyWhenExplicitlyEnabled()
    {
        using var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["DemoSeed:Enabled"] = "true",
            ["DemoSeed:Password"] = "PortfolioDemo!2026",
        });
        using var scope = serviceProvider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var manager = await userManager.FindByEmailAsync(PortfolioDemoAccounts.ManagerEmail);
        var agent = await userManager.FindByEmailAsync(PortfolioDemoAccounts.SupportAgentEmail);

        Assert.NotNull(manager);
        Assert.NotNull(agent);
        Assert.Contains("Manager", await userManager.GetRolesAsync(manager));
        Assert.Contains("SupportAgent", await userManager.GetRolesAsync(agent));
    }

    private static ServiceProvider CreateServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 4;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<DatabaseSeeder>();

        return services.BuildServiceProvider();
    }
}
