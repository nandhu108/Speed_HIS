using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Insurance;

/// <summary>
/// Claim submission → review → settlement/denial. Settlement follows the "explicit raise +
/// record collection" precedent (Finance's Payable/Receivable), posting through
/// IFinanceLedgerPoster.PostClaimSettlementAsync rather than auto-hooking at submission time.
/// </summary>
public class ClaimService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity,
    IUnitOfWork unitOfWork, IFileStorageService files, IFinanceLedgerPoster ledgerPoster, TimeProvider timeProvider)
{
    public async Task<long> SubmitAsync(SubmitClaimRequest request, CancellationToken ct = default)
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
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == request.BillId, ct)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);
        if (bill.Status == BillStatus.Cancelled)
        {
            throw new DomainException("A cancelled bill cannot be claimed.");
        }
        if (request.PreAuthRequestId is { } preAuthId &&
            !await db.PreAuthRequests.AnyAsync(p => p.Id == preAuthId, ct))
        {
            throw new NotFoundException(nameof(PreAuthRequest), preAuthId);
        }

        var now = timeProvider.GetUtcNow();
        var year = now.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.ClaimNumber, branchId, year, ct);
        var claim = new Claim
        {
            BranchId = branchId,
            ClaimNumber = $"CLM-{year}-{number.ToString().PadLeft(6, '0')}",
            PatientId = request.PatientId,
            PolicyId = request.PolicyId,
            PreAuthRequestId = request.PreAuthRequestId,
            BillId = request.BillId,
            ClaimAmount = request.ClaimAmount,
            Status = ClaimStatus.Submitted,
            SubmittedByUserId = currentUser.UserId,
            SubmittedAt = now,
        };
        db.Claims.Add(claim);
        bill.InsuranceCompanyId = policy.InsuranceCompanyId;
        await activity.LogAsync(ActivityEvents.ClaimSubmitted,
            $"Claim {claim.ClaimNumber} submitted against bill {bill.BillNumber} for {request.ClaimAmount:0.00}", ct: ct);
        await db.SaveChangesAsync(ct);
        return claim.Id;
    }

    public async Task UpdateStatusAsync(long id, UpdateClaimStatusRequest request, CancellationToken ct = default)
    {
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Claim), id);

        var allowed = claim.Status switch
        {
            ClaimStatus.Submitted => request.Status is ClaimStatus.UnderReview or ClaimStatus.Denied,
            ClaimStatus.UnderReview => request.Status is ClaimStatus.PartiallyApproved or ClaimStatus.Approved or ClaimStatus.Denied,
            _ => false,
        };
        if (!allowed)
        {
            throw new DomainException($"Claim {claim.ClaimNumber} cannot move from {claim.Status} to {request.Status}.");
        }

        if (request.Status is ClaimStatus.PartiallyApproved or ClaimStatus.Approved)
        {
            claim.ApprovedAmount = request.ApprovedAmount
                ?? throw new DomainException("Provide the approved amount.");
        }
        if (request.Status == ClaimStatus.Denied)
        {
            claim.DenialReasonId = request.DenialReasonId;
            claim.DenialNotes = request.DenialNotes;
        }

        claim.Status = request.Status;
        if (request.Status is ClaimStatus.Approved or ClaimStatus.PartiallyApproved or ClaimStatus.Denied)
        {
            claim.DecidedByUserId = currentUser.UserId;
            claim.DecidedAt = timeProvider.GetUtcNow();
        }

        await activity.LogAsync(ActivityEvents.ClaimStatusUpdated,
            $"Claim {claim.ClaimNumber} moved to {request.Status}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordSettlementAsync(
        long id, RecordClaimSettlementRequest request, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == id, token)
                ?? throw new NotFoundException(nameof(Claim), id);
            if (claim.Status is not (ClaimStatus.Approved or ClaimStatus.PartiallyApproved))
            {
                throw new DomainException($"Only an approved claim can be settled (currently {claim.Status}).");
            }
            if (!await db.PaymentModes.AnyAsync(m => m.Id == request.PaymentModeId, token))
            {
                throw new NotFoundException(nameof(PaymentMode), request.PaymentModeId);
            }

            claim.SettledAmount = request.SettledAmount;
            claim.DeductionAmount = request.DeductionAmount;
            claim.SettlementReferenceNumber = request.SettlementReferenceNumber;
            claim.SettledAt = timeProvider.GetUtcNow();
            claim.Status = ClaimStatus.Settled;

            await ledgerPoster.PostClaimSettlementAsync(
                claim.Id, claim.ClaimNumber, request.PaymentModeId, request.SettledAmount,
                DateOnly.FromDateTime(claim.SettledAt.Value.UtcDateTime), token);

            await activity.LogAsync(ActivityEvents.ClaimSettled,
                $"Claim {claim.ClaimNumber} settled for {request.SettledAmount:0.00}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task<long> AddDocumentAsync(
        long claimId, AddClaimDocumentRequest request, Stream content, string fileName, string contentType,
        long sizeBytes, CancellationToken ct = default)
    {
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw new NotFoundException(nameof(Claim), claimId);
        if (!await db.ClaimDocumentTypes.AnyAsync(d => d.Id == request.DocumentTypeId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Masters.ClaimDocumentType), request.DocumentTypeId);
        }

        var stored = await files.SaveAsync(content, $"claims/{claim.ClaimNumber}", fileName, ct);
        var document = new ClaimDocument
        {
            ClaimId = claimId,
            DocumentTypeId = request.DocumentTypeId,
            FileName = fileName,
            FilePath = stored.RelativePath,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            UploadedByUserId = currentUser.UserId,
            UploadedAt = timeProvider.GetUtcNow(),
        };
        db.ClaimDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document.Id;
    }

    public async Task<IReadOnlyList<ClaimDocumentDto>> GetDocumentsAsync(long claimId, CancellationToken ct = default) =>
        await db.ClaimDocuments.AsNoTracking()
            .Where(d => d.ClaimId == claimId)
            .Include(d => d.DocumentType)
            .OrderByDescending(d => d.Id)
            .Select(d => new ClaimDocumentDto(
                d.Id, d.DocumentTypeId, d.DocumentType!.Name, d.FileName, d.ContentType, d.SizeBytes, d.UploadedAt))
            .ToListAsync(ct);

    public async Task<(Stream Content, string ContentType, string FileName)> OpenDocumentAsync(
        long claimId, long documentId, CancellationToken ct = default)
    {
        var document = await db.ClaimDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ClaimId == claimId, ct)
            ?? throw new NotFoundException(nameof(ClaimDocument), documentId);
        var stream = await files.OpenReadAsync(document.FilePath, ct);
        return (stream, document.ContentType, document.FileName);
    }

    // QueryDto() used to return an already-projected IQueryable<ClaimDto> (a positional record
    // built through 6 joined tables), and GetPendingAsync/GetByIdAsync filtered *that* by
    // .Status/.Id — EF Core can't translate a filter reaching back through a record constructor
    // spanning that many joins ("The LINQ expression ... could not be translated"), so this 500'd
    // on every call to the Claims worklist. Filtering the raw entity (a simple, directly-mapped
    // column) before projecting to the DTO fixes it — same root cause and fix shape as
    // PreAuthService's GetPendingAsync and BillingReportService's GroupBy-after-Select failures.
    public async Task<IReadOnlyList<ClaimDto>> GetPendingAsync(CancellationToken ct = default) =>
        await Query()
            .Where(c => c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Denied)
            .OrderByDescending(c => c.SubmittedAt).Select(ToDto).ToListAsync(ct);

    public async Task<ClaimDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await Query().Where(c => c.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException(nameof(Claim), id);

    private IQueryable<Claim> Query() =>
        db.Claims.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Policy).ThenInclude(p => p!.InsuranceCompany)
            .Include(c => c.Bill)
            .Include(c => c.SubmittedByUser)
            .Include(c => c.DecidedByUser)
            .Include(c => c.DenialReason);

    private static readonly Expression<Func<Claim, ClaimDto>> ToDto = c => new ClaimDto(
        c.Id, c.ClaimNumber, c.PatientId, c.Patient!.Uhid, c.Patient!.FirstName + " " + c.Patient!.LastName,
        c.PolicyId, c.Policy!.PolicyNumber, c.Policy!.InsuranceCompany!.Name, c.PreAuthRequestId,
        c.BillId, c.Bill!.BillNumber, c.ClaimAmount, c.ApprovedAmount, c.Status,
        c.SubmittedByUser != null ? c.SubmittedByUser.FullName : null, c.SubmittedAt,
        c.DecidedByUser != null ? c.DecidedByUser.FullName : null, c.DecidedAt,
        c.DenialReasonId, c.DenialReason != null ? c.DenialReason.Name : null, c.DenialNotes,
        c.SettledAmount, c.DeductionAmount, c.SettlementReferenceNumber, c.SettledAt,
        c.Bill!.PaidAmount, c.Bill!.BalanceAmount);
}
