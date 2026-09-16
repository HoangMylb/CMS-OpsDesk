using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Infrastructure.Seeding;
using OpsDesk.Web.Authorization;
using OpsDesk.Web.Filters;
using OpsDesk.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Keep migrations assembly in Infrastructure, not Web.
            // This lets us run migrations from the Infrastructure project
            // without the Web project needing to know the DB details.
            sqlOptions.MigrationsAssembly("OpsDesk.Infrastructure");
        }
    ));

// ============================================================
// IDENTITY & AUTHENTICATION
// ============================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy — reasonable for an internal system.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;

        // Lockout settings — protect against brute force.
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Email must be unique — Identity enforces this via UserName = Email.
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ============================================================
// COOKIE AUTHENTICATION
// ============================================================
// Why cookies, not JWT?
// This is a server-rendered MVC application. The browser automatically
// sends the cookie on every request. JWT requires JavaScript to store
// the token and add it to every request header — unnecessary complexity
// for a server-side app. Cookies are the correct tool here.

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Standard workday session
    options.Cookie.HttpOnly = true;   // Not accessible from JavaScript (XSS protection)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only in production
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
});

// Cấu hình JWT Bearer song song cho các API client / Mobile apps
builder.Services.AddAuthentication()
    .AddJwtBearer(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "OpsDeskSecretSecurityKeyForJwtAuthentication2026!@#$%^OpsDeskSuperSecretKey";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OpsDesk",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OpsDeskClients",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ============================================================
// AUTHORIZATION — Policy-based with dynamic permission policies
// ============================================================

// Replace the default policy provider with ours so that any policy name
// that isn't pre-registered is treated as a permission string.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// Register the handler that checks whether the user's claims contain the permission.
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization();

// ============================================================
// APPLICATION SERVICES & CACHE & UNIT OF WORK
// ============================================================

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Unit of Work pattern (Transaction boundary management)
builder.Services.AddScoped<OpsDesk.Core.Data.IUnitOfWork, OpsDesk.Infrastructure.Data.UnitOfWork>();

// Cache & JWT Token services
builder.Services.AddScoped<OpsDesk.Core.Services.ICacheService, OpsDesk.Infrastructure.Services.MemoryCacheService>();
builder.Services.AddScoped<OpsDesk.Core.Services.IJwtService, OpsDesk.Infrastructure.Services.JwtService>();

// Domain Business services
builder.Services.AddScoped<OpsDesk.Core.Services.IAuditService, OpsDesk.Infrastructure.Services.AuditService>();
builder.Services.AddScoped<OpsDesk.Core.Services.IEmployeeService, OpsDesk.Infrastructure.Services.EmployeeService>();
builder.Services.AddScoped<OpsDesk.Core.Services.IRoleService, OpsDesk.Infrastructure.Services.RoleService>();
builder.Services.AddScoped<OpsDesk.Core.Services.ICustomerService, OpsDesk.Infrastructure.Services.CustomerService>();
builder.Services.AddScoped<OpsDesk.Core.Services.ISlaService, OpsDesk.Infrastructure.Services.SlaService>();
builder.Services.AddScoped<OpsDesk.Core.Services.ITicketService, OpsDesk.Infrastructure.Services.TicketService>();
builder.Services.AddScoped<OpsDesk.Core.Services.ITicketWorkflowService, OpsDesk.Infrastructure.Services.TicketWorkflowService>();
builder.Services.AddScoped<OpsDesk.Core.Services.ITicketMessageService, OpsDesk.Infrastructure.Services.TicketMessageService>();
builder.Services.AddScoped<OpsDesk.Core.Services.IDashboardService, OpsDesk.Infrastructure.Services.DashboardService>();

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews(options =>
{
    // Global anti-forgery filter
    var policy = new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()
    );
    options.Filters.Add(policy);

    // Global filter kiểm tra IsActive mỗi request.
    // Đây là cơ chế đảm bảo nhân viên bị vô hiệu hóa không thể tiếp tục dùng hệ thống
    // dù cookie của họ vẫn còn hiệu lực.
    options.Filters.Add<ActiveUserFilter>();
});

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// Order matters in ASP.NET Core middleware.
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // In production: generic error page without stack traces.
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Custom 404/403 pages — UseStatusCodePagesWithReExecute re-runs the pipeline
// through the specified path, which means our error controllers render proper views.
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();   // serve wwwroot (Bootstrap, CSS, JS)
app.UseRouting();

// Authentication must come before Authorization.
// Without UseAuthentication, the cookie is never parsed and User is always anonymous.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// ============================================================
// DATABASE INITIALISATION (Development only)
// ============================================================
// Seeding will be added in Phase 19.
// For now we just ensure the database is created/migrated on startup.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment())
    {
        await db.Database.MigrateAsync();

        // Seed dữ liệu demo — chỉ trong development
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}

app.Run();
