using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Infrastructure.Identity;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private static readonly JsonWebTokenHandler Handler = new();

    public int RefreshTokenDays => _options.RefreshTokenDays;
    public int PortalRefreshTokenDays => _options.PortalRefreshTokenDays;

    public AccessTokenResult CreateAccessToken(
        long userId, string username, string fullName, long branchId, Guid sessionId,
        IEnumerable<string> roleCodes)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, username),
            new("fullname", fullName),
            new("branch", branchId.ToString()),
            new("sid", sessionId.ToString()),
        };
        claims.AddRange(roleCodes.Select(code => new Claim(ClaimTypes.Role, code)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessTokenResult(Handler.CreateToken(descriptor), expiresAt);
    }

    public AccessTokenResult CreatePortalAccessToken(long patientId, string uhid, string fullName, Guid sessionId)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.PortalAccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, patientId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, uhid),
            new("fullname", fullName),
            new("uhid", uhid),
            new("sid", sessionId.ToString()),
            new("portal", "true"),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.PortalAudience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessTokenResult(Handler.CreateToken(descriptor), expiresAt);
    }

    public (string Token, string TokenHash) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
