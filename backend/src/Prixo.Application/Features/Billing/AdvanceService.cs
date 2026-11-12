using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// Patient deposits collected ahead of a procedure/admission. AdmissionId (Phase 9) tags a
/// deposit to a specific stay — AdmissionService.AdmitAsync sets it for the initial deposit,
/// and this same collector can be used again mid-stay for a top-up. Drawing an advance down
/// against a bill (BillingService.DrawAdvanceAsync) and refunding an advance's unused balance
/// (BillingService.RequestAdvanceRefundAsync/IssueRefundAsync) both live in BillingService
/// instead of here, since they're variations of billing workflows Refund/DrawAdvance already
/// own; TransferAsync stays here since it never touches a Bill at all.
/// </summary>
public class AdvanceService(
    IAppDbContext db, IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
    : IAdvanceCollector
{
    async Task<long> IAdvanceCollector.CollectAsync(
        long patientId, long? admissionId, decimal amount, long paymentModeId, string? referenceNumber,
        CancellationToken ct) =>
        await CollectAsync(new CollectAdvanceRequest(patientId, admissionId, amount, paymentModeId, referenceNumber, null), ct);

    public async Task<long> CollectAsync(CollectAdvanceRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch first.");
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }
        var mode = await db.PaymentModes.FirstOrDefaultAsync(m => m.Id == request.PaymentModeId, ct)
            ?? throw new NotFoundException(nameof(PaymentMode), request.PaymentModeId);
        if (mode.RequiresReference && string.IsNullOrWhiteSpace(request.ReferenceNumber))
        {
            throw new DomainException($"{mode.Name} payments require a reference number.");
        }

        if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var advance = new Advance
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            AdmissionId = request.AdmissionId,
            PaymentModeId = request.PaymentModeId,
            Amount = request.Amount,
            BalanceAmount = request.Amount,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            ReceivedAt = timeProvider.GetUtcNow(),
        };
        db.Advances.Add(advance);
        await db.SaveChangesAsync(ct);
        return advance.Id;
    }

    public async Task<IReadOnlyList<AdvanceDto>> GetByPatientAsync(long patientId, CancellationToken ct = default) =>
        await db.Advances.AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ReceivedAt)
            .Select(a => new AdvanceDto(
                a.Id, a.PatientId, a.Patient!.Uhid, a.Patient!.FirstName + " " + a.Patient!.LastName, a.AdmissionId,
                a.Amount, a.BalanceAmount, a.PaymentModeId, a.PaymentMode!.Name, a.ReferenceNumber, a.Notes, a.ReceivedAt))
            .ToListAsync(ct);

    /// <summary>Moves unused balance to a different patient/admission — see TransferAdvanceRequest.</summary>
    public async Task<long> TransferAsync(long advanceId, TransferAdvanceRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch first.");
            var source = await db.Advances.FirstOrDefaultAsync(a => a.Id == advanceId, token)
                ?? throw new NotFoundException(nameof(Advance), advanceId);
            if (request.Amount <= 0 || request.Amount > source.BalanceAmount)
            {
                throw new DomainException("Transfer amount must be positive and cannot exceed the advance's remaining balance.");
            }
            if (!await db.Patients.AnyAsync(p => p.Id == request.TargetPatientId, token))
            {
                throw new NotFoundException(nameof(Patient), request.TargetPatientId);
            }
            if (request.TargetAdmissionId is { } targetAdmissionId && !await db.Admissions.AnyAsync(a => a.Id == targetAdmissionId, token))
            {
                throw new NotFoundException(nameof(Admission), targetAdmissionId);
            }
            if (request.TargetPatientId == source.PatientId && request.TargetAdmissionId == source.AdmissionId)
            {
                throw new DomainException("Target patient/admission is the same as the source — nothing to transfer.");
            }

            source.BalanceAmount -= request.Amount;
            var target = new Advance
            {
                BranchId = branchId,
                PatientId = request.TargetPatientId,
                AdmissionId = request.TargetAdmissionId,
                PaymentModeId = source.PaymentModeId,
                Amount = request.Amount,
                BalanceAmount = request.Amount,
                ReferenceNumber = source.ReferenceNumber,
                Notes = request.Notes ?? $"Transferred from advance #{source.Id}",
                ReceivedAt = timeProvider.GetUtcNow(),
            };
            db.Advances.Add(target);

            await activity.LogAsync(ActivityEvents.AdvanceTransferred,
                $"Advance #{source.Id} transferred {request.Amount:0.00} to patient {request.TargetPatientId}"
                + (request.TargetAdmissionId is { } admId ? $" (admission {admId})" : ""), ct: token);
            await db.SaveChangesAsync(token);
            return target.Id;
        }, ct);
}
