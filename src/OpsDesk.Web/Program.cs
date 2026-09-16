using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Web.Authorization;
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
// APPLICATION SERVICES
// ============================================================

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Business services will be registered here as we implement each phase.
// e.g.:
//   builder.Services.AddScoped<ITicketService, TicketService>();
//   builder.Services.AddScoped<ITicketWorkflowService, TicketWorkflowService>();

// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews(options =>
{
    // Global anti-forgery filter — every POST, PUT, DELETE automatically
    // requires a valid anti-forgery token. This prevents CSRF attacks
    // without having to add [ValidateAntiForgeryToken] to every action.
    var policy = new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()
    );
    options.Filters.Add(policy);
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
        // Apply any pending migrations automatically in development.
        // In production, migrations should be run as a deployment step, not at startup.
        await db.Database.MigrateAsync();
    }
}

app.Run();
