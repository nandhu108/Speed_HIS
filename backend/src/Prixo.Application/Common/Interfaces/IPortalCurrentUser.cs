namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Ambient identity of the current Patient Portal request (plan §21's separate JWT audience).
/// Deliberately not part of ICurrentUser — a portal principal's "sub" claim is a PatientId,
/// never a staff UserId, so the two must never be conflated (see CurrentUserService's
/// defensive "portal" claim check for the failure mode this avoids).
/// </summary>
public interface IPortalCurrentUser
{
    bool IsAuthenticated { get; }
    long PatientId { get; }
    Guid? SessionId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
