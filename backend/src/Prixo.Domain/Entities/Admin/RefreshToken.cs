using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// Stored hashed (SHA-256). Rotated on every refresh; reuse of a revoked token
/// revokes the whole session (theft detection).
/// </summary>
public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SessionId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsUsable(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
