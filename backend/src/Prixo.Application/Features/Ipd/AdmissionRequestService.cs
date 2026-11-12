using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// Pre-admission request → approve/reject workflow gating AdmissionService.AdmitAsync,
/// mirroring Stores' indent→approve→issue pattern (plan §16's "Admission Request").
/// </summary>
public class AdmissionRequestService(IAppDbContext db, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(CreateAdmissionRequestRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);
        if (request.RequestedConsultantId is { } doctorId && !await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Masters.Doctor), doctorId);
        }
        if (request.WardId is { } wardId && !await db.Wards.AnyAsync(w => w.Id == wardId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Masters.Ward), wardId);
        }

        var admissionRequest = new AdmissionRequest
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            RequestedConsultantId = request.RequestedConsultantId,
            WardId = request.WardId,
            ExpectedAdmissionDate = request.ExpectedAdmissionDate,
            Reason = request.Reason,
            Status = AdmissionRequestStatus.Pending,
            RequestedByUserId = currentUser.UserId,
        };
        db.AdmissionRequests.Add(admissionRequest);

        await activity.LogAsync(ActivityEvents.AdmissionRequestCreated,
            $"Admission requested for patient '{patient.Uhid}'", ct: ct);
        await db.SaveChangesAsync(ct);
        return admissionRequest.Id;
    }

    public async Task<PagedResult<AdmissionRequestDto>> GetPagedAsync(AdmissionRequestQuery query, CancellationToken ct = default)
    {
        var requests = db.AdmissionRequests.AsNoTracking();
        if (query.Status is { } status) requests = requests.Where(r => r.Status == status);

        var totalCount = await requests.CountAsync(ct);
        var items = await requests
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new AdmissionRequestDto(
                r.Id, r.PatientId, r.Patient!.Uhid, r.Patient.FirstName + " " + r.Patient.LastName,
                r.RequestedConsultantId, r.RequestedConsultant != null ? r.RequestedConsultant.User!.FullName : null,
                r.WardId, r.Ward != null ? r.Ward.Name : null, r.ExpectedAdmissionDate, r.Reason,
                r.Status, r.RejectionReason,
                r.RequestedByUser!.FullName, r.CreatedAt,
                r.DecidedByUser != null ? r.DecidedByUser.FullName : null, r.DecidedAt, r.ApprovedAdmissionId))
            .ToListAsync(ct);

        return PagedResult<AdmissionRequestDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task ApproveAsync(long id, CancellationToken ct = default)
    {
        var request = await db.AdmissionRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(AdmissionRequest), id);
        EnsurePending(request);

        request.Status = AdmissionRequestStatus.Approved;
        request.DecidedByUserId = currentUser.UserId;
        request.DecidedAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.AdmissionRequestApproved, $"Admission request #{id} approved", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(long id, RejectAdmissionRequestRequest body, CancellationToken ct = default)
    {
        var request = await db.AdmissionRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(AdmissionRequest), id);
        EnsurePending(request);

        request.Status = AdmissionRequestStatus.Rejected;
        request.RejectionReason = body.Reason;
        request.DecidedByUserId = currentUser.UserId;
        request.DecidedAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.AdmissionRequestRejected,
            $"Admission request #{id} rejected: {body.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    private static void EnsurePending(AdmissionRequest request)
    {
        if (request.Status != AdmissionRequestStatus.Pending)
        {
            throw new DomainException($"Admission request has already been {request.Status.ToString().ToLowerInvariant()}.");
        }
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
