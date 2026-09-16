using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles, IEnumerable<Claim> permissionClaims)
    {
        var secretKey = _config["Jwt:Key"] ?? "OpsDeskSecretSecurityKeyForJwtAuthentication2026!@#$%^";
        var issuer = _config["Jwt:Issuer"] ?? "OpsDesk";
        var audience = _config["Jwt:Audience"] ?? "OpsDeskClients";
        var expiresMinutes = int.TryParse(_config["Jwt:ExpiresInMinutes"], out var exp) ? exp : 480;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("departmentId", user.DepartmentId?.ToString() ?? string.Empty),
            new("isActive", user.IsActive.ToString().ToLower())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var p in permissionClaims)
        {
            if (p.Type == "Permission")
            {
                claims.Add(new Claim("Permission", p.Value));
            }
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
