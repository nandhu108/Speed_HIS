using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Application.Common.Security;

/// <summary>
/// Resolves "which doctor is the caller" for clinical attribution and ownership checks. A doctor
/// is a User with a linked Doctor profile; administrators, nurses and front-desk staff have none.
/// Used so that notes, orders and e-signatures are attributed to the authenticated doctor rather
/// than to a doctorId supplied in the request body.
/// </summary>
public static class ClinicalActor
{
    /// <summary>The caller's Doctor id, or null when the caller is not a doctor.</summary>
    public static Task<long?> GetCallerDoctorIdAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken ct) =>
        db.Doctors.AsNoTracking().Where(d => d.UserId == currentUser.UserId).Select(d => (long?)d.Id).FirstOrDefaultAsync(ct);

    /// <summary>
    /// When the caller is a doctor, the supplied doctor id must be their own (a doctor cannot act or
    /// sign in another doctor's name). Non-doctor callers (e.g. a nurse transcribing a verbal order)
    /// may record on behalf of a doctor. Returns the doctor id to attribute the action to.
    /// </summary>
    public static async Task<long> ResolveAttributedDoctorAsync(
        IAppDbContext db, ICurrentUser currentUser, long requestedDoctorId, CancellationToken ct)
    {
        var callerDoctorId = await GetCallerDoctorIdAsync(db, currentUser, ct);
        if (callerDoctorId is { } mine && mine != requestedDoctorId)
        {
            throw new ForbiddenException("You can only record this in your own name.");
        }
        return requestedDoctorId;
    }
}
