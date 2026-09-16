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
            ["AdminUsers:0:UserName"] = "IncomSaiGon",
            ["AdminUsers:0:Email"] = "admin@incom.vn",
            ["AdminUsers:0:Password"] = "Incom@2026",
            ["AdminUsers:0:Roles:0"] = "ADMIN",
            ["AdminUsers:0:Roles:1"] = "SUPER_ADMIN",
            ["AdminUsers:1:UserName"] = "MiniAppCore",
            ["AdminUsers:1:Email"] = "admin@MiniAppCore.vn",
            ["AdminUsers:1:Password"] = "Incom@2026",
            ["AdminUsers:1:Roles:0"] = "ADMIN",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
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

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Act
        await seeder.SeedAsync();

        // Assert - Roles created
        Assert.True(await roleManager.RoleExistsAsync("ADMIN"));
        Assert.True(await roleManager.RoleExistsAsync("SUPER_ADMIN"));

        // Assert - Users created
        var incomUser = await userManager.FindByEmailAsync("admin@incom.vn");
        Assert.NotNull(incomUser);
        Assert.Equal("IncomSaiGon", incomUser.UserName);
        Assert.True(incomUser.IsActive);
        Assert.True(incomUser.EmailConfirmed);

        var incomRoles = await userManager.GetRolesAsync(incomUser);
        Assert.Contains(incomRoles, r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(incomRoles, r => r.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase));

        var miniAppUser = await userManager.FindByEmailAsync("admin@MiniAppCore.vn");
        Assert.NotNull(miniAppUser);
        Assert.Equal("MiniAppCore", miniAppUser.UserName);
        Assert.True(miniAppUser.IsActive);

        var miniAppRoles = await userManager.GetRolesAsync(miniAppUser);
        Assert.Contains(miniAppRoles, r => r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase));
    }
}
