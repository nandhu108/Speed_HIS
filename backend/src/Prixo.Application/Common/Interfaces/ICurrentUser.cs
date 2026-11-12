namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Ambient identity of the current request. Populated from JWT claims by the API layer;
/// falls back to the system user (id 1) for unauthenticated/background work so that
/// audit stamping always has an actor.
/// </summary>
public interface ICurrentUser
{
    long UserId { get; }
    long? BranchId { get; }
    string? UserName { get; }
    Guid? SessionId { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? TraceId { get; }
}
