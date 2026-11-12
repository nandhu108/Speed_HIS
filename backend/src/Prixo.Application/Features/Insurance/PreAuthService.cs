using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Insurance;

/// <summary>
/// Pre-authorization request → approve/reject, plus mid-treatment "enhancement" requests
/// against an already-approved pre-auth. Modeled on Ipd.AdmissionRequestService's strict
/// single-step state machine (EnsurePending-style guards).
/// </summary>
public class PreAuthService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity,
    TimeProvider timeProvider)
{
    public async Task<long> RaiseAsync(RaisePreAuthRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch first.");
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }
        var policy = await db.PatientInsurancePolicies.FirstOrDefaultAsync(p => p.Id == request.PolicyId, ct)
            ?? throw new NotFoundException(nameof(PatientInsurancePolicy), request.PolicyId);
        if (policy.PatientId != request.PatientId)
        {
            throw new DomainException("The selected policy does not belong to this patient.");
        }
        if (request.OpVisitId is { } opVisitId && !await db.OpVisits.AnyAsync(v => v.Id == opVisitId, ct))
        {
            throw new NotFoundException(nameof(OpVisit), opVisitId);
        }
        if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var now = timeProvider.GetUtcNow();
        var year = now.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.PreAuthNumber, branchId, year, ct);
        var preAuth = new PreAuthRequest
        {
            BranchId = branchId,
            PreAuthNumber = $"PA-{year}-{number.ToString().PadLeft(6, '0')}",
            PatientId = request.PatientId,
            PolicyId = request.PolicyId,
            TreatmentType = request.TreatmentType,
            OpVisitId = request.OpVisitId,
            AdmissionId = request.AdmissionId,
            EstimatedAmount = request.EstimatedAmount,
            Diagnosis = request.Diagnosis,
            RequestedByUserId = currentUser.UserId,
            RequestedAt = now,
        };
        db.PreAuthRequests.Add(preAuth);
        await activity.LogAsync(ActivityEvents.PreAuthRequested,
            $"Pre-auth {preAuth.PreAuthNumber} requested for patient {request.PatientId}", ct: ct);
        await db.SaveChangesAsync(ct);
        return preAuth.Id;
    }

    public async Task DecideAsync(long id, DecidePreAuthRequest request, CancellationToken ct = default)
    {
        var preAuth = await db.PreAuthRequests.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PreAuthRequest), id);
        EnsureRequested(preAuth);

        if (request.Approve)
        {
            var approvedAmount = request.ApprovedAmount
                ?? throw new DomainException("Provide the approved amount.");
            preAuth.ApprovedAmount = approvedAmount;
            preAuth.Status = approvedAmount >= preAuth.EstimatedAmount
                ? PreAuthStatus.Approved
                : PreAuthStatus.PartiallyApproved;
            await activity.LogAsync(ActivityEvents.PreAuthApproved,
                $"Pre-auth {preAuth.PreAuthNumber} approved for {approvedAmount:0.00}", ct: ct);
        }
        else
        {
            preAuth.Status = PreAuthStatus.Rejected;
            preAuth.RejectionReason = request.RejectionReason;
            await activity.LogAsync(ActivityEvents.PreAuthRejected,
                $"Pre-auth {preAuth.PreAuthNumber} rejected: {request.RejectionReason}", ct: ct);
        }
        preAuth.DecidedByUserId = currentUser.UserId;
        preAuth.DecidedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
    }

    public async Task<long> RequestEnhancementAsync(
        long preAuthId, RequestEnhancementRequest request, CancellationToken ct = default)
    {
        var preAuth = await db.PreAuthRequests.FirstOrDefaultAsync(p => p.Id == preAuthId, ct)
            ?? throw new NotFoundException(nameof(PreAuthRequest), preAuthId);
        if (preAuth.Status is not (PreAuthStatus.Approved or PreAuthStatus.PartiallyApproved))
        {
            throw new DomainException("Only an approved pre-auth can request an enhancement.");
        }

        var enhancement = new PreAuthEnhancement
        {
            PreAuthRequestId = preAuthId,
            RequestedAmount = request.RequestedAmount,
            Reason = request.Reason,
            RequestedByUserId = currentUser.UserId,
            RequestedAt = timeProvider.GetUtcNow(),
        };
        db.PreAuthEnhancements.Add(enhancement);
        await activity.LogAsync(ActivityEvents.PreAuthEnhancementRequested,
            $"Enhancement of {request.RequestedAmount:0.00} requested on pre-auth {preAuth.PreAuthNumber}", ct: ct);
        await db.SaveChangesAsync(ct);
        return enhancement.Id;
    }

    public async Task DecideEnhancementAsync(
        long enhancementId, DecideEnhancementRequest request, CancellationToken ct = default)
    {
        var enhancement = await db.PreAuthEnhancements.Include(e => e.PreAuthRequest)
            .FirstOrDefaultAsync(e => e.Id == enhancementId, ct)
            ?? throw new NotFoundException(nameof(PreAuthEnhancement), enhancementId);
        if (enhancement.Status != PreAuthEnhancementStatus.Requested)
        {
            throw new DomainException($"This enhancement is already {enhancement.Status}.");
        }

        if (request.Approve)
        {
            enhancement.Status = PreAuthEnhancementStatus.Approved;
            var preAuth = enhancement.PreAuthRequest!;
            preAuth.ApprovedAmount = (preAuth.ApprovedAmount ?? 0) + enhancement.RequestedAmount;
            preAuth.Status = PreAuthStatus.Approved;
            await activity.LogAsync(ActivityEvents.PreAuthEnhancementApproved,
                $"Enhancement of {enhancement.RequestedAmount:0.00} approved on pre-auth {preAuth.PreAuthNumber}", ct: ct);
        }
        else
        {
            enhancement.Status = PreAuthEnhancementStatus.Rejected;
            enhancement.RejectionReason = request.RejectionReason;
            await activity.LogAsync(ActivityEvents.PreAuthEnhancementRejected,
                $"Enhancement request {enhancementId} rejected: {request.RejectionReason}", ct: ct);
        }
        enhancement.DecidedByUserId = currentUser.UserId;
        enhancement.DecidedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
    }

    // GetPendingAsync/GetApprovedAsync/GetByIdAsync used to filter QueryDto()'s already-projected
    // IQueryable<PreAuthRequestDto> (a positional record built from 5 joined tables) by .Status/
    // .Id — EF Core's query compiler can't decompose a filter that reaches back through a
    // record constructor spanning that many joins ("The LINQ expression ... could not be
    // translated"), so GetPendingAsync 500'd on every call. Filtering the raw entity first (this
    // is a simple, directly-mapped column) and only projecting to the DTO afterwards fixes it —
    // same root cause and fix shape as BillingReportService's GroupBy-after-Select failures.
    public async Task<IReadOnlyList<PreAuthRequestDto>> GetPendingAsync(CancellationToken ct = default) =>
        await Query().Where(p => p.Status == PreAuthStatus.Requested)
            .OrderBy(p => p.RequestedAt).Select(ToDto).ToListAsync(ct);

    /// <summary>
    /// Approved/PartiallyApproved pre-auths — the only ones an "enhancement" (mid-treatment
    /// amount increase) can be requested against. Deciding a pre-auth moves it out of
    /// GetPendingAsync's Requested-only worklist, so without this query staff would have no way
    /// to find an already-approved pre-auth again to raise or decide an enhancement on it.
    /// </summary>
    public async Task<IReadOnlyList<PreAuthRequestDto>> GetApprovedAsync(CancellationToken ct = default) =>
        await Query().Where(p => p.Status == PreAuthStatus.Approved || p.Status == PreAuthStatus.PartiallyApproved)
            .OrderByDescending(p => p.RequestedAt).Select(ToDto).ToListAsync(ct);

    public async Task<PreAuthRequestDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await Query().Where(p => p.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException(nameof(PreAuthRequest), id);

    public async Task<IReadOnlyList<PreAuthEnhancementDto>> GetEnhancementsAsync(
        long preAuthId, CancellationToken ct = default) =>
        await db.PreAuthEnhancements.AsNoTracking()
            .Where(e => e.PreAuthRequestId == preAuthId)
            .Include(e => e.RequestedByUser)
            .Include(e => e.DecidedByUser)
            .OrderByDescending(e => e.RequestedAt)
            .Select(e => new PreAuthEnhancementDto(
                e.Id, e.PreAuthRequestId, e.RequestedAmount, e.Reason,
                e.RequestedByUser!.FullName, e.RequestedAt,
                e.Status, e.DecidedByUser != null ? e.DecidedByUser.FullName : null, e.DecidedAt, e.RejectionReason))
            .ToListAsync(ct);

    private IQueryable<PreAuthRequest> Query() =>
        db.PreAuthRequests.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Policy).ThenInclude(policy => policy!.InsuranceCompany)
            .Include(p => p.RequestedByUser)
            .Include(p => p.DecidedByUser);

    private static readonly Expression<Func<PreAuthRequest, PreAuthRequestDto>> ToDto = p => new PreAuthRequestDto(
        p.Id, p.PreAuthNumber, p.PatientId, p.Patient!.Uhid,
        p.Patient!.FirstName + " " + p.Patient!.LastName,
        p.PolicyId, p.Policy!.PolicyNumber, p.Policy!.InsuranceCompany!.Name,
        p.TreatmentType, p.OpVisitId, p.AdmissionId, p.EstimatedAmount, p.Diagnosis,
        p.RequestedByUser!.FullName, p.RequestedAt,
        p.Status, p.ApprovedAmount,
        p.DecidedByUser != null ? p.DecidedByUser.FullName : null, p.DecidedAt, p.RejectionReason);

    private static void EnsureRequested(PreAuthRequest preAuth)
    {
        if (preAuth.Status != PreAuthStatus.Requested)
        {
            throw new DomainException($"This pre-auth request is already {preAuth.Status}.");
        }
    }
}
