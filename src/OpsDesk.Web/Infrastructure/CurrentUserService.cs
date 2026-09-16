using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace OpsDesk.Web.Infrastructure;

/// <summary>
/// Provides convenient access to the currently authenticated user's ID and claims
/// without passing HttpContext into services.
///
/// Why not just inject IHttpContextAccessor everywhere?
/// Injecting IHttpContextAccessor into business services couples them to HTTP
/// concepts, making them harder to test and reason about.
/// CurrentUserService wraps that concern and provides a clean, typed interface.
///
/// Services that need the current user ID receive ICurrentUserService,
/// not IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        User?.Claims.Any(c => c.Type == "Permission" && c.Value == permission) ?? false;
}
