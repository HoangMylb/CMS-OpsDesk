using System.Security.Claims;
using OpsDesk.Core.Entities;

namespace OpsDesk.Core.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles, IEnumerable<Claim> permissionClaims);
}
