using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Masters.Departments;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// Admission: request → IP number → bed allocation → consultant mapping → consent →
/// initial deposit → admission slip (plan §9). Consent capture and deposit collection are
/// inlined here (a plain PatientConsent/Advance row) rather than calling PatientService/
/// AdvanceService directly — same cross-module posture as Billing reading OpVisit straight
/// off the DbContext instead of injecting OpVisitService. Admit/transfer/cancel are the
/// CLAUDE.md-mandated transactional operations ("admission" is explicitly named); placing a
/// secondary consultant is a plain single save since it touches nothing else.
/// </summary>
public class AdmissionService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences, ICurrentUser currentUser, IPdfService pdf, TimeProvider timeProvider)
{
    public async Task<long> AdmitAsync(AdmitPatientRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            var patientState = await db.Patients.Where(p => p.Id == request.PatientId).Select(p => new { p.IsBlocked, p.BlockedReason }).FirstOrDefaultAsync(token)
                ?? throw new NotFoundException(nameof(Patient), request.PatientId);
            if (patientState.IsBlocked)
            {
                throw new DomainException($"This patient is blocked ({patientState.BlockedReason}). Unblock the patient before admitting.");
            }
            // One active admission per patient — serialise concurrent admits of the same patient.
            await unitOfWork.LockAsync<Patient>(request.PatientId, token);
            var active = await db.Admissions.IgnoreQueryFilters()
                .Where(x => !x.IsDeleted && x.PatientId == request.PatientId && x.Status == AdmissionStatus.Admitted)
                .Select(x => x.AdmissionNumber).FirstOrDefaultAsync(token);
            if (active is not null)
            {
                throw new DomainException($"This patient is already admitted ({active}). Transfer the existing admission instead of admitting again.");
            }
            if (!await db.Doctors.AnyAsync(d => d.Id == request.PrimaryConsultantId, token))
            {
                throw new NotFoundException(nameof(Doctor), request.PrimaryConsultantId);
            }
            var department = await DepartmentService.EnsureClinicalAsync(
                db, request.DepartmentId ?? throw new DomainException("Select the department for this admission."), token);
            if (request.ReferringEmergencyVisitId is { } erVisitId && !await db.EmergencyVisits.AnyAsync(v => v.Id == erVisitId, token))
            {
                throw new NotFoundException(nameof(Domain.Entities.Er.EmergencyVisit), erVisitId);
            }
            var bed = await db.Beds.Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.Room).ThenInclude(r => r!.Ward)
                .FirstOrDefaultAsync(b => b.Id == request.BedId, token)
                ?? throw new NotFoundException(nameof(Bed), request.BedId);
            if (bed.Status != BedStatus.Vacant)
            {
                throw new DomainException($"Bed {bed.Code} is not vacant ({bed.Status}).");
            }

            AdmissionRequest? admissionRequest = null;
            if (request.AdmissionRequestId is { } admissionRequestId)
            {
                admissionRequest = await db.AdmissionRequests.FirstOrDefaultAsync(r => r.Id == admissionRequestId, token)
                    ?? throw new NotFoundException(nameof(AdmissionRequest), admissionRequestId);
                if (admissionRequest.Status != AdmissionRequestStatus.Approved)
                {
                    throw new DomainException("Admission request must be approved before admitting.");
                }
                if (admissionRequest.ApprovedAdmissionId is not null)
                {
                    throw new DomainException("Admission request has already been admitted.");
                }
                if (admissionRequest.PatientId != request.PatientId)
                {
                    throw new DomainException("Admission request is for a different patient.");
                }
            }

            var now = timeProvider.GetUtcNow();
            var year = now.Year;
            var number = await sequences.NextValueAsync(SequenceKeys.AdmissionNumber, branchId, year, token);
            var admission = new Admission
            {
                BranchId = branchId,
                PatientId = request.PatientId,
                AdmissionNumber = $"IP-{year}-{number.ToString().PadLeft(6, '0')}",
                AdmissionDate = now,
                AdmissionType = request.AdmissionType,
                PrimaryConsultantId = request.PrimaryConsultantId,
                DepartmentId = department.Id,
                ReferringOpVisitId = request.ReferringOpVisitId,
                ReferringEmergencyVisitId = request.ReferringEmergencyVisitId,
                CurrentBedId = bed.Id,
                ProvisionalDiagnosis = request.ProvisionalDiagnosis,
                Status = AdmissionStatus.Admitted,
            };

            admission.Consultants.Add(new AdmissionConsultant
            {
                DoctorId = request.PrimaryConsultantId, Role = ConsultantRole.Primary, AssignedAt = now,
            });
            foreach (var doctorId in request.SecondaryConsultantIds.Distinct())
            {
                if (doctorId == request.PrimaryConsultantId) continue;
                if (!await db.Doctors.AnyAsync(d => d.Id == doctorId, token))
                {
                    throw new NotFoundException(nameof(Doctor), doctorId);
                }
                admission.Consultants.Add(new AdmissionConsultant { DoctorId = doctorId, Role = ConsultantRole.Secondary, AssignedAt = now });
            }

            var bedChargePerDay = await ResolveBedChargePerDayAsync(bed.Room?.RoomTypeId, bed.Room?.RoomType?.BedChargePerDay ?? 0, now, token);
            admission.BedAllocations.Add(new BedAllocation
            {
                BedId = bed.Id, BedChargePerDay = bedChargePerDay, AllocatedAt = now,
            });

            if (request.ConsentTemplateId is { } templateId)
            {
                if (string.IsNullOrWhiteSpace(request.ConsentSignedByName))
                {
                    throw new DomainException("Consent signed-by name is required when capturing admission consent.");
                }
                if (!await db.ConsentTemplates.AnyAsync(c => c.Id == templateId, token))
                {
                    throw new NotFoundException(nameof(ConsentTemplate), templateId);
                }
                var consent = new PatientConsent
                {
                    PatientId = request.PatientId,
                    ConsentTemplateId = templateId,
                    SignedByName = request.ConsentSignedByName!,
                    RelationshipToPatient = request.ConsentRelationshipToPatient,
                    SignedAt = now,
                };
                db.PatientConsents.Add(consent);
                await db.SaveChangesAsync(token);
                admission.PatientConsentId = consent.Id;
            }

            bed.Status = BedStatus.Occupied;
            db.Admissions.Add(admission);
            await db.SaveChangesAsync(token);

            if (request.InitialDepositAmount > 0)
            {
                if (request.DepositPaymentModeId is not { } paymentModeId)
                {
                    throw new DomainException("Select a payment mode to collect the initial deposit.");
                }
                var mode = await db.PaymentModes.FirstOrDefaultAsync(m => m.Id == paymentModeId, token)
                    ?? throw new NotFoundException(nameof(PaymentMode), paymentModeId);
                if (mode.RequiresReference && string.IsNullOrWhiteSpace(request.DepositReferenceNumber))
                {
                    throw new DomainException($"{mode.Name} payments require a reference number.");
                }
                db.Advances.Add(new Advance
                {
                    BranchId = branchId,
                    PatientId = request.PatientId,
                    AdmissionId = admission.Id,
                    PaymentModeId = paymentModeId,
                    Amount = request.InitialDepositAmount,
                    BalanceAmount = request.InitialDepositAmount,
                    ReferenceNumber = request.DepositReferenceNumber,
                    Notes = "IP admission initial deposit",
                    ReceivedAt = now,
                });
                await db.SaveChangesAsync(token);
            }

            foreach (var clearanceType in Enum.GetValues<ClearanceType>())
            {
                db.DischargeClearances.Add(new DischargeClearance { AdmissionId = admission.Id, ClearanceType = clearanceType, IsCleared = false });
            }

            if (admissionRequest is not null)
            {
                admissionRequest.ApprovedAdmissionId = admission.Id;
            }

            await db.SaveChangesAsync(token);

            return admission.Id;
        }, ct);

    public async Task<PagedResult<AdmissionListItemDto>> GetPagedAsync(AdmissionQuery query, CancellationToken ct = default)
    {
        var admissions = db.Admissions.AsNoTracking();
        if (query.PatientId is { } patientId) admissions = admissions.Where(a => a.PatientId == patientId);
        if (query.Status is { } status) admissions = admissions.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            admissions = admissions.Where(a =>
                EF.Functions.ILike(a.AdmissionNumber, term) ||
                EF.Functions.ILike(a.Patient!.Uhid, term) ||
                EF.Functions.ILike(a.Patient!.FirstName, term) ||
                EF.Functions.ILike(a.Patient!.LastName, term));
        }

        var totalCount = await admissions.CountAsync(ct);
        var items = await admissions
            .OrderByDescending(a => a.AdmissionDate).ThenByDescending(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(a => new AdmissionListItemDto(
                a.Id, a.AdmissionNumber, a.AdmissionDate, a.PatientId, a.Patient!.Uhid,
                a.Patient!.FirstName + " " + a.Patient!.LastName, a.AdmissionType, a.Status,
                a.PrimaryConsultant!.User!.FullName, a.CurrentBed!.Room!.Ward!.Name, a.CurrentBed.Room.Name, a.CurrentBed.Code))
            .ToListAsync(ct);
        return PagedResult<AdmissionListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdmissionDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var a = await db.Admissions.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.PrimaryConsultant).ThenInclude(d => d!.User)
            .Include(x => x.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .Include(x => x.Consultants).ThenInclude(c => c.Doctor).ThenInclude(d => d!.User)
            .Include(x => x.BedAllocations).ThenInclude(ba => ba.Bed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .Include(x => x.Department)
            .Include(x => x.ReferringOpVisit)
            .Include(x => x.ReferringEmergencyVisit)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(Admission), id);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return new AdmissionDetailDto(
            a.Id, a.AdmissionNumber, a.AdmissionDate, a.PatientId, a.Patient!.Uhid,
            a.Patient.FirstName + " " + a.Patient.LastName, a.Patient.Gender.ToString(),
            IpdMath.FormatAge(a.Patient.DateOfBirth, a.Patient.EstimatedAge, today), a.AdmissionType,
            a.ProvisionalDiagnosis, a.PrimaryConsultantId, a.PrimaryConsultant!.User!.FullName,
            a.ReferringOpVisitId, a.ReferringOpVisit?.VisitNumber, a.ReferringEmergencyVisitId, a.ReferringEmergencyVisit?.VisitNumber,
            a.Status, a.CurrentBedId, a.CurrentBed!.Room!.Ward!.Name, a.CurrentBed.Room.Name, a.CurrentBed.Code,
            a.DischargeRequestedAt, a.DischargeType, a.DischargeTypeNotes, a.DischargedAt, a.CancelReason, a.CreditLimit,
            a.Consultants.OrderByDescending(c => c.AssignedAt)
                .Select(c => new AdmissionConsultantDto(c.Id, c.DoctorId, c.Doctor!.User!.FullName, c.Role, c.AssignedAt, c.EndedAt, c.Notes))
                .ToList(),
            a.BedAllocations.OrderByDescending(ba => ba.AllocatedAt)
                .Select(ba => new BedAllocationDto(ba.Id, ba.BedId, ba.Bed!.Room!.Ward!.Name, ba.Bed.Room.Name, ba.Bed.Code,
                    ba.BedChargePerDay, ba.AllocatedAt, ba.ReleasedAt, ba.TransferReason))
                .ToList(),
            a.DepartmentId, a.Department?.Name);
    }

    public async Task TransferBedAsync(long admissionId, TransferBedRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var admission = await db.Admissions.Include(a => a.BedAllocations)
                .FirstOrDefaultAsync(a => a.Id == admissionId, token) ?? throw new NotFoundException(nameof(Admission), admissionId);
            if (admission.Status != AdmissionStatus.Admitted)
            {
                throw new DomainException("Only an active admission can be bed-transferred.");
            }
            if (request.NewBedId == admission.CurrentBedId)
            {
                throw new DomainException("Patient is already in that bed.");
            }
            var newBed = await db.Beds.Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .FirstOrDefaultAsync(b => b.Id == request.NewBedId, token) ?? throw new NotFoundException(nameof(Bed), request.NewBedId);
            if (newBed.Status != BedStatus.Vacant)
            {
                throw new DomainException($"Bed {newBed.Code} is not vacant ({newBed.Status}).");
            }
            var oldBed = await db.Beds.FirstOrDefaultAsync(b => b.Id == admission.CurrentBedId, token)
                ?? throw new NotFoundException(nameof(Bed), admission.CurrentBedId);

            var now = timeProvider.GetUtcNow();
            var current = admission.BedAllocations.First(ba => ba.ReleasedAt == null);
            current.ReleasedAt = now;
            current.TransferReason = request.Reason;
            oldBed.Status = BedStatus.Vacant;

            var newBedChargePerDay = await ResolveBedChargePerDayAsync(newBed.Room?.RoomTypeId, newBed.Room?.RoomType?.BedChargePerDay ?? 0, now, token);
            admission.BedAllocations.Add(new BedAllocation
            {
                BedId = newBed.Id, BedChargePerDay = newBedChargePerDay, AllocatedAt = now,
            });
            newBed.Status = BedStatus.Occupied;
            admission.CurrentBedId = newBed.Id;

            await db.SaveChangesAsync(token);
        }, ct);

    public async Task TransferConsultantAsync(long admissionId, TransferConsultantRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var admission = await db.Admissions.Include(a => a.Consultants)
                .FirstOrDefaultAsync(a => a.Id == admissionId, token) ?? throw new NotFoundException(nameof(Admission), admissionId);
            if (admission.Status != AdmissionStatus.Admitted)
            {
                throw new DomainException("Only an active admission can have its consultant transferred.");
            }
            if (request.NewPrimaryConsultantId == admission.PrimaryConsultantId)
            {
                throw new DomainException("That doctor is already the primary consultant.");
            }
            if (!await db.Doctors.AnyAsync(d => d.Id == request.NewPrimaryConsultantId, token))
            {
                throw new NotFoundException(nameof(Doctor), request.NewPrimaryConsultantId);
            }

            var now = timeProvider.GetUtcNow();
            var current = admission.Consultants.FirstOrDefault(c => c.Role == ConsultantRole.Primary && c.EndedAt == null);
            if (current is not null) current.EndedAt = now;

            admission.Consultants.Add(new AdmissionConsultant
            {
                DoctorId = request.NewPrimaryConsultantId, Role = ConsultantRole.Primary, AssignedAt = now, Notes = request.Notes,
            });
            admission.PrimaryConsultantId = request.NewPrimaryConsultantId;

            await db.SaveChangesAsync(token);
        }, ct);

    public async Task AddSecondaryConsultantAsync(long admissionId, AddSecondaryConsultantRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.DoctorId);
        }
        if (await db.AdmissionConsultants.AnyAsync(c => c.AdmissionId == admissionId && c.DoctorId == request.DoctorId && c.EndedAt == null, ct))
        {
            throw new DomainException("That doctor is already an active consultant on this admission.");
        }
        db.AdmissionConsultants.Add(new AdmissionConsultant
        {
            AdmissionId = admissionId, DoctorId = request.DoctorId, Role = ConsultantRole.Secondary,
            AssignedAt = timeProvider.GetUtcNow(), Notes = request.Notes,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task EndSecondaryConsultantAsync(long admissionId, long consultantId, CancellationToken ct = default)
    {
        var consultant = await db.AdmissionConsultants.FirstOrDefaultAsync(c => c.Id == consultantId && c.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException(nameof(AdmissionConsultant), consultantId);
        if (consultant.Role != ConsultantRole.Secondary)
        {
            throw new DomainException("Use the transfer-consultant action for the primary consultant.");
        }
        consultant.EndedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long admissionId, CancelAdmissionRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var admission = await db.Admissions.Include(a => a.BedAllocations)
                .FirstOrDefaultAsync(a => a.Id == admissionId, token) ?? throw new NotFoundException(nameof(Admission), admissionId);
            if (admission.Status != AdmissionStatus.Admitted)
            {
                throw new DomainException("Only an active admission can be cancelled.");
            }
            if (admission.DischargeRequestedAt is not null)
            {
                throw new DomainException("Discharge is already in progress for this admission.");
            }
            var ipBills = await db.Bills.Where(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled).ToListAsync(token);
            if (ipBills.Any(b => b.TotalAmount > 0 || b.PaidAmount > 0))
            {
                throw new DomainException(
                    $"IP bill {ipBills.First(b => b.TotalAmount > 0 || b.PaidAmount > 0).BillNumber} already carries charges or payments. Refund and cancel it before cancelling the admission.");
            }
            foreach (var empty in ipBills)
            {
                empty.Status = BillStatus.Cancelled;
                empty.CancelReason = $"Admission cancelled: {request.Reason}";
                empty.CancelledAt = timeProvider.GetUtcNow();
            }

            var bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == admission.CurrentBedId, token)
                ?? throw new NotFoundException(nameof(Bed), admission.CurrentBedId);
            var now = timeProvider.GetUtcNow();
            var current = admission.BedAllocations.First(ba => ba.ReleasedAt == null);
            current.ReleasedAt = now;
            bed.Status = BedStatus.Vacant;

            admission.Status = AdmissionStatus.Cancelled;
            admission.CancelReason = request.Reason;
            await db.SaveChangesAsync(token);
        }, ct);

    /// <summary>Sets or clears the front desk's IP credit-limit warning threshold (null = no ceiling tracked).</summary>
    public async Task SetCreditLimitAsync(long admissionId, SetCreditLimitRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        admission.CreditLimit = request.CreditLimit;
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GenerateAdmissionSlipAsync(long admissionId, CancellationToken ct = default)
    {
        var a = await db.Admissions.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.PrimaryConsultant).ThenInclude(d => d!.User)
            .Include(x => x.CurrentBed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .FirstOrDefaultAsync(x => x.Id == admissionId, ct) ?? throw new NotFoundException(nameof(Admission), admissionId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == a.BranchId, ct);
        var deposit = await db.Advances.AsNoTracking().Where(ad => ad.AdmissionId == admissionId).SumAsync(ad => (decimal?)ad.Amount, ct) ?? 0;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return pdf.GenerateAdmissionSlip(new AdmissionSlipData(
            a.AdmissionNumber, a.Patient!.FirstName + " " + a.Patient.LastName, a.Patient.Uhid, a.Patient.Gender.ToString(),
            IpdMath.FormatAge(a.Patient.DateOfBirth, a.Patient.EstimatedAge, today), branch?.Name ?? string.Empty,
            branch?.Address ?? string.Empty, a.AdmissionDate, a.AdmissionType.ToString(), a.CurrentBed!.Room!.Ward!.Name,
            a.CurrentBed.Room.Name, a.CurrentBed.Code, a.PrimaryConsultant!.User!.FullName, deposit));
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    /// <summary>Resolves the room type's date-effective daily charge as of the allocation moment, falling back to the RoomType's flat BedChargePerDay if no RoomTypeCharge row is effective — snapshotted onto BedAllocation.BedChargePerDay so a later rate change never rewrites this stay's history.</summary>
    private async Task<decimal> ResolveBedChargePerDayAsync(long? roomTypeId, decimal fallbackRate, DateTimeOffset asOf, CancellationToken ct)
    {
        if (roomTypeId is not { } id)
        {
            return fallbackRate;
        }
        var candidates = await db.RoomTypeCharges.AsNoTracking().Where(c => c.RoomTypeId == id)
            .Select(c => new RoomTypeChargeCandidate(c.EffectiveFrom, c.EffectiveTo, c.Rate))
            .ToListAsync(ct);
        return IpdMath.ResolveRoomTypeCharge(candidates, DateOnly.FromDateTime(asOf.UtcDateTime), fallbackRate);
    }
}
