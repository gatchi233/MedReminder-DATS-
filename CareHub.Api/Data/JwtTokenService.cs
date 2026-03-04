using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareHub.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CareHub.Api.Data;

public sealed class JwtTokenService
{
    private readonly AuthOptions _options;
    private readonly CareHubDbContext _db;

    public JwtTokenService(IOptions<AuthOptions> options, CareHubDbContext db)
    {
        _options = options.Value;
        _db = db;
    }

    public async Task<AuthUser?> TryValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var dbUser = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == username.ToLower(), ct);

        if (dbUser is not null && VerifyPassword(password, dbUser.PasswordHash))
        {
            return new AuthUser
            {
                Username = dbUser.Username,
                Password = password,
                Role = dbUser.Role,
                DisplayName = dbUser.DisplayName,
                ResidentId = dbUser.ResidentId?.ToString()
            };
        }

        return _options.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);
    }

    public (string token, DateTimeOffset expiresAtUtc) CreateToken(AuthUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.TokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.ResidentId))
            claims.Add(new("resident_id", user.ResidentId));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>
    /// Supports both plaintext passwords (from DataSeedService) and BCrypt hashes (from DevController).
    /// </summary>
    private static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        // BCrypt hashes start with "$2a$", "$2b$", or "$2y$"
        if (storedHash.StartsWith("$2"))
        {
            try { return BCrypt.Net.BCrypt.Verify(password, storedHash); }
            catch { return false; }
        }

        // Plaintext comparison
        return storedHash == password;
    }
}
