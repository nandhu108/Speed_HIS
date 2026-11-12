using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Er;

/// <summary>The MLC Register and the Accident Case Register (plan §16) — two distinct statutory records, since a visit can be an accident without being medico-legal and vice versa.</summary>
public class MlcService(IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> SaveMlcAsync(long visitId, SaveMlcRequest request, CancellationToken ct = default)
    {
        var visit = await db.EmergencyVisits.FirstOrDefaultAsync(v => v.Id == visitId, ct) ?? throw new NotFoundException(nameof(EmergencyVisit), visitId);
        var record = await db.MlcRecords.FirstOrDefaultAsync(m => m.EmergencyVisitId == visitId, ct);
        if (record is null)
        {
            var branchId = RequireBranch();
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var number = await sequences.NextValueAsync(SequenceKeys.MlcNumber, branchId, today.Year, ct);
            record = new MlcRecord
            {
                EmergencyVisitId = visitId, MlcNumber = $"MLC-{today.Year}-{number.ToString().PadLeft(6, '0')}", RecordedByUserId = currentUser.UserId,
            };
            db.MlcRecords.Add(record);
            visit.IsMlc = true;
        }
        record.PoliceStationName = request.PoliceStationName;
        record.PoliceInformedAt = request.PoliceInformedAt;
        record.FirNumber = request.FirNumber;
        record.BroughtByName = request.BroughtByName;
        record.BroughtByDesignation = request.BroughtByDesignation;
        record.InjuryDescription = request.InjuryDescription;
        record.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return record.Id;
    }

    public async Task<MlcRecordDto> GetMlcAsync(long visitId, CancellationToken ct = default)
    {
        var m = await db.MlcRecords.AsNoTracking().Include(x => x.RecordedByUser)
            .FirstOrDefaultAsync(x => x.EmergencyVisitId == visitId, ct) ?? throw new NotFoundException(nameof(MlcRecord), visitId);
        return new MlcRecordDto(m.Id, m.EmergencyVisitId, m.MlcNumber, m.PoliceStationName, m.PoliceInformedAt, m.FirNumber,
            m.BroughtByName, m.BroughtByDesignation, m.InjuryDescription, m.Notes, m.RecordedByUser!.FullName);
    }

    public async Task<long> SaveAccidentAsync(long visitId, SaveAccidentRequest request, CancellationToken ct = default)
    {
        if (!await db.EmergencyVisits.AnyAsync(v => v.Id == visitId, ct))
        {
            throw new NotFoundException(nameof(EmergencyVisit), visitId);
        }
        var record = await db.AccidentCaseRecords.FirstOrDefaultAsync(a => a.EmergencyVisitId == visitId, ct);
        if (record is null)
        {
            record = new AccidentCaseRecord { EmergencyVisitId = visitId, AccidentType = request.AccidentType, RecordedByUserId = currentUser.UserId };
            db.AccidentCaseRecords.Add(record);
        }
        record.AccidentType = request.AccidentType;
        record.AccidentAt = request.AccidentAt;
        record.AccidentLocation = request.AccidentLocation;
        record.VehicleInvolved = request.VehicleInvolved;
        record.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return record.Id;
    }

    public async Task<AccidentCaseRecordDto> GetAccidentAsync(long visitId, CancellationToken ct = default)
    {
        var a = await db.AccidentCaseRecords.AsNoTracking().Include(x => x.RecordedByUser)
            .FirstOrDefaultAsync(x => x.EmergencyVisitId == visitId, ct) ?? throw new NotFoundException(nameof(AccidentCaseRecord), visitId);
        return new AccidentCaseRecordDto(a.Id, a.EmergencyVisitId, a.AccidentType, a.AccidentAt, a.AccidentLocation, a.VehicleInvolved, a.Notes, a.RecordedByUser!.FullName);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
