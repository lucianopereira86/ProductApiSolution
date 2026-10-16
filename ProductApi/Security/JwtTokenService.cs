using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    private readonly IConfiguration _configuration = configuration;
    public string GenerateToken(string userId, string username, string role, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
          new(JwtRegisteredClaimNames.Sub, userId),
          new("name", username),
          new("role", role),
          new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(30);

        var token = new JwtSecurityToken(jwtIssuer, jwtAudience, claims, null, expires, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}