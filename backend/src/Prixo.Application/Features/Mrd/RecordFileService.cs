using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// Medical Record File Creation + File Tracking (plan §17). One RecordFile per Admission
/// (enforced by a partial unique index); location/custodian changes are logged as an
/// append-only <see cref="FileMovement"/> trail and mirrored onto the file's current snapshot.
/// </summary>
public class RecordFileService(IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(CreateRecordFileRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == request.AdmissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), request.AdmissionId);
        if (await db.RecordFiles.AnyAsync(f => f.AdmissionId == request.AdmissionId, ct))
        {
            throw new DomainException("A medical record file already exists for this admission.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var number = await sequences.NextValueAsync(SequenceKeys.RecordFileNumber, admission.BranchId, today.Year, ct);
        var file = new RecordFile
        {
            BranchId = admission.BranchId,
            AdmissionId = admission.Id,
            FileNumber = $"MR-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            CreatedByUserId = currentUser.UserId,
        };
        db.RecordFiles.Add(file);
        await activity.LogAsync(ActivityEvents.MrdFileCreated, $"Medical record file {file.FileNumber} created", ct: ct);
        await db.SaveChangesAsync(ct);
        return file.Id;
    }

    public async Task<IReadOnlyList<RecordFileListItemDto>> GetListAsync(RecordFileQuery query, CancellationToken ct = default)
    {
        var files = db.RecordFiles.AsNoTracking()
            .Include(f => f.Admission).ThenInclude(a => a!.Patient)
            .Include(f => f.CurrentCustodianUser)
            .AsQueryable();
        if (query.Status is { } status) files = files.Where(f => f.Status == status);
        if (query.AdmissionId is { } admissionId) files = files.Where(f => f.AdmissionId == admissionId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            files = files.Where(f => EF.Functions.ILike(f.FileNumber, term)
                || EF.Functions.ILike(f.Admission!.Patient!.FirstName, term)
                || EF.Functions.ILike(f.Admission!.Patient!.LastName, term)
                || EF.Functions.ILike(f.Admission!.Patient!.Uhid, term));
        }

        var rows = await files.OrderByDescending(f => f.CreatedAt).ToListAsync(ct);
        return rows.Select(ToListItemDto).ToList();
    }

    public async Task<RecordFileDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var f = await db.RecordFiles.AsNoTracking()
            .Include(x => x.Admission).ThenInclude(a => a!.Patient)
            .Include(x => x.CurrentCustodianUser)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(RecordFile), id);

        return new RecordFileDetailDto(
            f.Id, f.FileNumber, f.Status, f.AdmissionId, f.Admission!.AdmissionNumber,
            f.Admission.PatientId, f.Admission.Patient!.FirstName + " " + f.Admission.Patient.LastName, f.Admission.Patient.Uhid,
            f.CurrentLocation, f.CurrentCustodianUserId, f.CurrentCustodianUser?.FullName, f.CreatedByUser!.FullName, f.CreatedAt, f.DestroyedAt);
    }

    public async Task<long> RecordMovementAsync(long id, RecordMovementRequest request, CancellationToken ct = default)
    {
        var file = await db.RecordFiles.FirstOrDefaultAsync(f => f.Id == id, ct) ?? throw new NotFoundException(nameof(RecordFile), id);
        if (file.Status != Domain.Enums.RecordFileStatus.Active)
        {
            throw new DomainException($"A file that is {file.Status} cannot be moved.");
        }

        var movement = new FileMovement
        {
            RecordFileId = id,
            MovedAt = timeProvider.GetUtcNow(),
            FromLocation = file.CurrentLocation,
            ToLocation = request.ToLocation,
            FromCustodianUserId = file.CurrentCustodianUserId,
            ToCustodianUserId = request.ToCustodianUserId,
            Reason = request.Reason,
            MovedByUserId = currentUser.UserId,
        };
        db.FileMovements.Add(movement);

        file.CurrentLocation = request.ToLocation;
        file.CurrentCustodianUserId = request.ToCustodianUserId;
        await db.SaveChangesAsync(ct);
        return movement.Id;
    }

    public async Task<IReadOnlyList<FileMovementDto>> GetMovementsAsync(long id, CancellationToken ct = default) =>
        await db.FileMovements.AsNoTracking().Where(m => m.RecordFileId == id)
            .Include(m => m.FromCustodianUser).Include(m => m.ToCustodianUser).Include(m => m.MovedByUser)
            .OrderByDescending(m => m.MovedAt)
            .Select(m => new FileMovementDto(
                m.Id, m.MovedAt, m.FromLocation, m.ToLocation,
                m.FromCustodianUser != null ? m.FromCustodianUser.FullName : null,
                m.ToCustodianUser != null ? m.ToCustodianUser.FullName : null,
                m.Reason, m.MovedByUser!.FullName))
            .ToListAsync(ct);

    private static RecordFileListItemDto ToListItemDto(RecordFile f) => new(
        f.Id, f.FileNumber, f.Status, f.AdmissionId, f.Admission!.AdmissionNumber,
        f.Admission.PatientId, f.Admission.Patient!.FirstName + " " + f.Admission.Patient.LastName, f.Admission.Patient.Uhid,
        f.CurrentLocation, f.CurrentCustodianUser?.FullName, f.CreatedAt);
}
