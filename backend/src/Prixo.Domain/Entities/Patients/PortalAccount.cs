using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Patient Portal login (plan §21: "Patient Login... password first, OTP when SMS integration
/// lands"). One row per Patient (self-service activated via RegisterAsync's mobile+UHID+DOB
/// identity match) — deliberately a separate table from Patient rather than widening it with
/// auth columns, same reasoning Admin's User/Employee split uses. Global, not branch-scoped —
/// patient identity is global.
/// </summary>
public class PortalAccount : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string PasswordHash { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

/// <summary>Rotating hashed refresh tokens for the portal JWT audience — same shape/reuse-detection posture as Admin's RefreshToken, just keyed to a Patient instead of a User.</summary>
public class PortalRefreshToken : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid SessionId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsUsable(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
