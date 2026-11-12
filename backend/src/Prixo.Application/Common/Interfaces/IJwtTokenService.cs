namespace Prixo.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(
        long userId, string username, string fullName, long branchId, Guid sessionId,
        IEnumerable<string> roleCodes);

    /// <summary>Portal audience token (plan §21) — carries a "portal" claim so CurrentUserService can refuse to treat the subject (a PatientId) as a staff UserId.</summary>
    AccessTokenResult CreatePortalAccessToken(long patientId, string uhid, string fullName, Guid sessionId);

    /// <summary>Generates a cryptographically random refresh token and its SHA-256 hash.</summary>
    (string Token, string TokenHash) CreateRefreshToken();

    string HashRefreshToken(string token);
    int RefreshTokenDays { get; }
    int PortalRefreshTokenDays { get; }
}
