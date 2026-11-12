using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// File Issue &amp; Return, and Record Requests — internal/external/legal (plan §17), one
/// service and table for both. Lifecycle: Requested → Approved/Rejected → Issued → Returned.
/// A record file can have at most one non-terminal (Requested/Approved/Issued) request open
/// at a time — enforced here, not by a unique index, since the terminal states must coexist
/// with history.
/// </summary>
public class FileRequestService(IAppDbContext db, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(CreateFileRequestRequest request, CancellationToken ct = default)
    {
        var file = await db.RecordFiles.FirstOrDefaultAsync(f => f.Id == request.RecordFileId, ct)
            ?? throw new NotFoundException(nameof(RecordFile), request.RecordFileId);
        if (file.Status != RecordFileStatus.Active)
        {
            throw new DomainException($"A file that is {file.Status} cannot be requested.");
        }
        if (await db.FileRequests.AnyAsync(r => r.RecordFileId == request.RecordFileId &&
            (r.Status == FileRequestStatus.Requested || r.Status == FileRequestStatus.Approved || r.Status == FileRequestStatus.Issued), ct))
        {
            throw new DomainException("This file already has an open request/issue in progress.");
        }

        var fileRequest = new FileRequest
        {
            RecordFileId = request.RecordFileId,
            RequestType = request.RequestType,
            RequestedByUserId = request.RequestType == FileRequestType.Internal ? currentUser.UserId : null,
            RequesterName = request.RequesterName,
            RequesterOrganization = request.RequesterOrganization,
            Purpose = request.Purpose,
            ExpectedReturnDate = request.ExpectedReturnDate,
            RequestedAt = timeProvider.GetUtcNow(),
        };
        db.FileRequests.Add(fileRequest);
        await db.SaveChangesAsync(ct);
        return fileRequest.Id;
    }

    public async Task ApproveAsync(long id, CancellationToken ct = default)
    {
        var request = await RequireAsync(id, ct);
        RequireStatus(request, FileRequestStatus.Requested);
        request.Status = FileRequestStatus.Approved;
        request.ApprovedByUserId = currentUser.UserId;
        request.ApprovedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(long id, RejectFileRequestRequest request, CancellationToken ct = default)
    {
        var fileRequest = await RequireAsync(id, ct);
        RequireStatus(fileRequest, FileRequestStatus.Requested);
        fileRequest.Status = FileRequestStatus.Rejected;
        fileRequest.ApprovedByUserId = currentUser.UserId;
        fileRequest.ApprovedAt = timeProvider.GetUtcNow();
        fileRequest.RejectReason = request.Reason;
        await db.SaveChangesAsync(ct);
    }

    public async Task IssueAsync(long id, CancellationToken ct = default)
    {
        var request = await RequireAsync(id, ct);
        RequireStatus(request, FileRequestStatus.Approved);
        var file = await db.RecordFiles.FirstAsync(f => f.Id == request.RecordFileId, ct);
        var now = timeProvider.GetUtcNow();

        request.Status = FileRequestStatus.Issued;
        request.IssuedByUserId = currentUser.UserId;
        request.IssuedAt = now;

        db.FileMovements.Add(new FileMovement
        {
            RecordFileId = file.Id, MovedAt = now, FromLocation = file.CurrentLocation,
            ToLocation = request.RequestType == FileRequestType.Internal ? "Issued (internal)" : "Issued (external/legal)",
            FromCustodianUserId = file.CurrentCustodianUserId, ToCustodianUserId = request.RequestedByUserId,
            Reason = $"File request #{request.Id}: {request.Purpose}", MovedByUserId = currentUser.UserId,
        });
        file.CurrentLocation = request.RequestType == FileRequestType.Internal ? "Issued (internal)" : "Issued (external/legal)";
        file.CurrentCustodianUserId = request.RequestedByUserId;

        await activity.LogAsync(ActivityEvents.MrdFileIssued, $"Medical record file {file.FileNumber} issued (request #{request.Id})", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReturnAsync(long id, CancellationToken ct = default)
    {
        var request = await RequireAsync(id, ct);
        RequireStatus(request, FileRequestStatus.Issued);
        var file = await db.RecordFiles.FirstAsync(f => f.Id == request.RecordFileId, ct);
        var now = timeProvider.GetUtcNow();

        request.Status = FileRequestStatus.Returned;
        request.ReturnedToUserId = currentUser.UserId;
        request.ReturnedAt = now;

        db.FileMovements.Add(new FileMovement
        {
            RecordFileId = file.Id, MovedAt = now, FromLocation = file.CurrentLocation, ToLocation = "MRD Storage",
            FromCustodianUserId = file.CurrentCustodianUserId, ToCustodianUserId = null,
            Reason = $"File request #{request.Id} returned", MovedByUserId = currentUser.UserId,
        });
        file.CurrentLocation = "MRD Storage";
        file.CurrentCustodianUserId = null;

        await activity.LogAsync(ActivityEvents.MrdFileReturned, $"Medical record file {file.FileNumber} returned (request #{request.Id})", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FileRequestDto>> GetQueueAsync(FileRequestQuery query, CancellationToken ct = default)
    {
        var requests = db.FileRequests.AsNoTracking()
            .Include(r => r.RecordFile).Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser).Include(r => r.IssuedByUser).Include(r => r.ReturnedToUser)
            .AsQueryable();
        if (query.Status is { } status) requests = requests.Where(r => r.Status == status);
        if (query.RecordFileId is { } recordFileId) requests = requests.Where(r => r.RecordFileId == recordFileId);

        var rows = await requests.OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dtos = rows.Select(r => ToDto(r, today));
        if (query.OverdueOnly) dtos = dtos.Where(d => d.IsOverdue);
        return dtos.ToList();
    }

    public async Task<FileRequestDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var r = await db.FileRequests.AsNoTracking()
            .Include(x => x.RecordFile).Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser).Include(x => x.IssuedByUser).Include(x => x.ReturnedToUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(FileRequest), id);
        return ToDto(r, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
    }

    private async Task<FileRequest> RequireAsync(long id, CancellationToken ct) =>
        await db.FileRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException(nameof(FileRequest), id);

    private static void RequireStatus(FileRequest request, FileRequestStatus expected)
    {
        if (request.Status != expected)
        {
            throw new DomainException($"This request is {request.Status}; expected {expected}.");
        }
    }

    private static FileRequestDto ToDto(FileRequest r, DateOnly today) => new(
        r.Id, r.RecordFileId, r.RecordFile!.FileNumber, r.RequestType,
        r.RequestedByUserId, r.RequestedByUser?.FullName, r.RequesterName, r.RequesterOrganization,
        r.Purpose, r.ExpectedReturnDate, r.Status, r.RequestedAt,
        r.ApprovedByUser?.FullName, r.ApprovedAt, r.RejectReason,
        r.IssuedByUser?.FullName, r.IssuedAt, r.ReturnedToUser?.FullName, r.ReturnedAt,
        r.Status == FileRequestStatus.Issued && MrdMath.IsOverdue(r.ExpectedReturnDate, today), r.Notes);
}
