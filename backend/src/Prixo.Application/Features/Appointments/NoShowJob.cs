using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Appointments;

/// <summary>
/// Auto-marks Scheduled appointments as NoShow once their slot has passed. Runs with no
/// HTTP context (Hangfire recurring job), so the branch query filter naturally disables
/// itself and every branch is covered in one pass — see AppDbContext.CurrentBranchId.
/// </summary>
public class NoShowJob(IAppDbContext db, IActivityLogger activity, TimeProvider timeProvider) : INoShowJob
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var currentTime = TimeOnly.FromDateTime(now.UtcDateTime);

        var overdue = await db.Appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled &&
                (a.AppointmentDate < today || (a.AppointmentDate == today && a.EndTime < currentTime)))
            .ToListAsync(ct);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var appointment in overdue)
        {
            appointment.Status = AppointmentStatus.NoShow;
            appointment.NoShowMarkedAt = now;
        }

        await activity.LogAsync(ActivityEvents.AppointmentsAutoMarkedNoShow,
            $"{overdue.Count} appointment(s) auto-marked no-show", ct: ct);
        await db.SaveChangesAsync(ct);
    }
}
