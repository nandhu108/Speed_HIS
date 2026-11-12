using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Appointments;

/// <summary>Weekly recurring availability plus date-specific overrides (leave/special hours), per branch.</summary>
public class DoctorScheduleService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<DoctorScheduleDto> GetByDoctorIdAsync(long doctorId, CancellationToken ct = default)
    {
        var doctor = await db.Doctors.AsNoTracking()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == doctorId, ct)
            ?? throw new NotFoundException(nameof(Doctor), doctorId);

        var blocks = await db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new DoctorScheduleBlockDto(s.Id, s.DayOfWeek, s.StartTime, s.EndTime, s.SlotMinutes))
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var overrides = await db.ScheduleOverrides.AsNoTracking()
            .Where(o => o.DoctorId == doctorId && o.Date >= today)
            .OrderBy(o => o.Date)
            .Select(o => new ScheduleOverrideDto(o.Id, o.Date, o.IsAvailable, o.StartTime, o.EndTime, o.SlotMinutes, o.Reason))
            .ToListAsync(ct);

        return new DoctorScheduleDto(doctor.Id, doctor.User!.FullName, blocks, overrides);
    }

    public async Task ReplaceBlocksAsync(long doctorId, ReplaceDoctorScheduleRequest request, CancellationToken ct = default)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), doctorId);
        }

        var existing = await db.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync(ct);
        db.DoctorSchedules.RemoveRange(existing);

        foreach (var block in request.Blocks)
        {
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctorId,
                BranchId = currentUser.BranchId ?? throw new DomainException("Select a branch first."),
                DayOfWeek = block.DayOfWeek,
                StartTime = block.StartTime,
                EndTime = block.EndTime,
                SlotMinutes = block.SlotMinutes,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddOverrideAsync(long doctorId, SaveScheduleOverrideRequest request, CancellationToken ct = default)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), doctorId);
        }
        if (await db.ScheduleOverrides.AnyAsync(o => o.DoctorId == doctorId && o.Date == request.Date, ct))
        {
            throw new DomainException($"An override for {request.Date:yyyy-MM-dd} already exists. Remove it first.");
        }

        var entity = new ScheduleOverride
        {
            DoctorId = doctorId,
            BranchId = currentUser.BranchId ?? throw new DomainException("Select a branch first."),
            Date = request.Date,
            IsAvailable = request.IsAvailable,
            StartTime = request.IsAvailable ? request.StartTime : null,
            EndTime = request.IsAvailable ? request.EndTime : null,
            SlotMinutes = request.IsAvailable ? request.SlotMinutes : null,
            Reason = request.Reason,
        };
        db.ScheduleOverrides.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteOverrideAsync(long doctorId, long overrideId, CancellationToken ct = default)
    {
        var entity = await db.ScheduleOverrides
            .FirstOrDefaultAsync(o => o.Id == overrideId && o.DoctorId == doctorId, ct)
            ?? throw new NotFoundException(nameof(ScheduleOverride), overrideId);
        db.ScheduleOverrides.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
