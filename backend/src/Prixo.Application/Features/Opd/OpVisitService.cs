using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Masters.Departments;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Opd;

public class OpVisitService(
    IAppDbContext db,
    ISequenceService sequences,
    ICurrentUser currentUser,
    IActivityLogger activity,
    IPdfService pdf,
    SettingService settings,
    IUnitOfWork unitOfWork,
    IAdvanceCollector advanceCollector,
    TimeProvider timeProvider)
{
    public async Task<OpVisitDetailDto> RegisterAsync(RegisterOpVisitRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch before registering a visit.");

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);
        if (patient.IsBlocked)
        {
            throw new DomainException($"This patient is blocked ({patient.BlockedReason}). Unblock the patient before registering a visit.");
        }
        var doctor = await db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.DoctorId, ct)
            ?? throw new NotFoundException(nameof(Doctor), request.DoctorId);

        // Department is mandatory for OP. Callers that go through the HTTP validator always send one;
        // the Camp CSV bulk import calls this method directly, so fall back to the doctor's first
        // mapped department rather than failing every row.
        var departmentId = request.DepartmentId
            ?? await db.DoctorDepartments.Where(dd => dd.DoctorId == request.DoctorId)
                .OrderBy(dd => dd.Id).Select(dd => (long?)dd.DepartmentId).FirstOrDefaultAsync(ct)
            ?? throw new DomainException("Select the department for this visit.");
        var department = await DepartmentService.EnsureClinicalAsync(db, departmentId, ct);

        Camp? camp = null;
        if (request.CampId is { } campId)
        {
            camp = await db.Camps.FirstOrDefaultAsync(c => c.Id == campId, ct)
                ?? throw new NotFoundException(nameof(Camp), campId);
        }

        VisitClass? visitClass = null;
        if (request.VisitClassId is { } visitClassId)
        {
            visitClass = await db.VisitClasses.FirstOrDefaultAsync(c => c.Id == visitClassId, ct)
                ?? throw new NotFoundException(nameof(VisitClass), visitClassId);
        }

        VisitSession? session = null;
        if (request.SessionId is { } sessionId)
        {
            session = await db.VisitSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
                ?? throw new NotFoundException(nameof(VisitSession), sessionId);
        }

        if (request.AdvanceAmount is { } advanceAmount && advanceAmount > 0 && request.PaymentModeId is null)
        {
            throw new DomainException("Select a payment mode to collect the advance.");
        }

        Appointment? appointment = null;
        if (request.AppointmentId is { } appointmentId)
        {
            appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
                ?? throw new NotFoundException(nameof(Appointment), appointmentId);
            if (appointment.PatientId != request.PatientId || appointment.DoctorId != request.DoctorId)
            {
                throw new DomainException("The appointment does not match the selected patient and doctor.");
            }
            if (await db.OpVisits.AnyAsync(v => v.AppointmentId == appointmentId, ct))
            {
                throw new DomainException("A visit has already been registered for this appointment.");
            }
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var cutoff = today.AddDays(-Math.Max(doctor.FollowUpWindowDays, 0));
        // <= today (not < today): a same-day repeat visit to the same doctor is a Review too.
        var hasRecentCompletedVisit = await db.OpVisits.AnyAsync(v =>
            v.PatientId == request.PatientId && v.DoctorId == request.DoctorId &&
            v.Status == OpVisitStatus.Completed && v.VisitDate >= cutoff && v.VisitDate <= today, ct);
        var visitType = hasRecentCompletedVisit ? VisitType.Review : VisitType.New;
        var consultationFee = visitType == VisitType.New ? doctor.ConsultationFee : doctor.FollowUpFee;
        // "Consultation-charge settings" — a doctor with no fee configured (0) falls back to the
        // hospital-wide default rather than billing nothing; a doctor with a real fee (even a
        // deliberately low one) is never overridden.
        if (consultationFee == 0)
        {
            consultationFee = visitType == VisitType.New
                ? await settings.GetDecimalAsync(SettingKeys.BillingDefaultConsultationFee, 0, ct)
                : await settings.GetDecimalAsync(SettingKeys.BillingDefaultFollowUpFee, 0, ct);
        }

        var year = now.Year;
        var visitNumberValue = await sequences.NextValueAsync(SequenceKeys.OpVisitNumber, branchId, year, ct);
        var visitNumber = $"OPD-{year}-{visitNumberValue.ToString().PadLeft(6, '0')}";

        // Reuse the appointment's token so the patient sees one number end to end;
        // a walk-in with no appointment gets a fresh one from the OPD sequence.
        int tokenNumber;
        if (appointment is not null)
        {
            tokenNumber = appointment.TokenNumber;
        }
        else
        {
            var tokenScope = EncodeDate(today);
            tokenNumber = (int)await sequences.NextValueAsync(SequenceKeys.OpVisitToken, request.DoctorId, tokenScope, ct);
        }

        var visit = new OpVisit
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            Patient = patient,
            DoctorId = request.DoctorId,
            Doctor = doctor,
            DepartmentId = department.Id,
            Department = department,
            CampId = request.CampId,
            Camp = camp,
            AppointmentId = request.AppointmentId,
            Appointment = appointment,
            VisitNumber = visitNumber,
            VisitDate = today,
            TokenNumber = tokenNumber,
            VisitType = visitType,
            ConsultationFee = consultationFee,
            BookingType = request.BookingType,
            VisitClassId = request.VisitClassId,
            VisitClass = visitClass,
            SessionId = request.SessionId,
            Session = session,
            IsMlcCase = request.IsMlcCase,
        };

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            db.OpVisits.Add(visit);
            await db.SaveChangesAsync(token);

            if (request.AdvanceAmount is { } advanceAmount && advanceAmount > 0)
            {
                await advanceCollector.CollectAsync(
                    request.PatientId, null, advanceAmount, request.PaymentModeId!.Value,
                    request.AdvanceReferenceNumber, token);
            }
        }, ct);

        return ToDetailDto(visit);
    }

    public async Task<PagedResult<OpVisitListItemDto>> GetPagedAsync(OpVisitQuery query, CancellationToken ct = default)
    {
        var visits = db.OpVisits.AsNoTracking().Where(v => v.Category == query.Category);

        if (query.DoctorId is { } doctorId) visits = visits.Where(v => v.DoctorId == doctorId);
        if (query.PatientId is { } patientId) visits = visits.Where(v => v.PatientId == patientId);
        if (query.CampId is { } campId) visits = visits.Where(v => v.CampId == campId);
        if (query.Date is { } date) visits = visits.Where(v => v.VisitDate == date);
        if (query.Status is { } status) visits = visits.Where(v => v.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            visits = visits.Where(v =>
                EF.Functions.ILike(v.Patient!.Uhid, term) ||
                EF.Functions.ILike(v.Patient!.FirstName, term) ||
                EF.Functions.ILike(v.Patient!.LastName, term) ||
                EF.Functions.ILike(v.VisitNumber, term));
        }

        var totalCount = await visits.CountAsync(ct);
        var items = await visits
            .OrderByDescending(v => v.VisitDate).ThenBy(v => v.TokenNumber)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(v => new OpVisitListItemDto(
                v.Id, v.VisitNumber, v.TokenNumber, v.PatientId, v.Patient!.Uhid,
                v.Patient!.FirstName + " " + v.Patient!.LastName,
                v.Category == VisitCategory.Opd ? v.DoctorId!.Value : v.MhcDoctorId!.Value,
                v.Category == VisitCategory.Opd ? v.Doctor!.User!.FullName : v.MhcDoctor!.Name,
                v.VisitDate, v.VisitType, v.Status, db.Admissions.Any(a => a.ReferringOpVisitId == v.Id),
                v.CampId, v.Camp != null ? v.Camp.Name : null))
            .ToListAsync(ct);

        return PagedResult<OpVisitListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<OpVisitDetailDto> RegisterMhcAsync(RegisterMhcVisitRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch before registering a visit.");

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);
        var mhcDoctor = await db.MhcDoctors.FirstOrDefaultAsync(d => d.Id == request.MhcDoctorId, ct)
            ?? throw new NotFoundException(nameof(MhcDoctor), request.MhcDoctorId);
        var package = await db.MhcPackages.FirstOrDefaultAsync(p => p.Id == request.MhcPackageId, ct)
            ?? throw new NotFoundException(nameof(MhcPackage), request.MhcPackageId);

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var cutoff = today.AddDays(-Math.Max(mhcDoctor.FollowUpWindowDays, 0));
        // Unlike OPD's Completed-gated rule, MHC visits never reach Completed in this pass (no
        // consultation workspace exists to complete them), so the Review signal is "any
        // non-cancelled prior visit" rather than "a completed one" — see docs/modules/opd.md.
        var hasRecentVisit = await db.OpVisits.AnyAsync(v =>
            v.Category == VisitCategory.Mhc && v.PatientId == request.PatientId && v.MhcDoctorId == request.MhcDoctorId &&
            v.Status != OpVisitStatus.Cancelled && v.VisitDate >= cutoff && v.VisitDate <= today, ct);
        var visitType = hasRecentVisit ? VisitType.Review : VisitType.New;

        var year = now.Year;
        var visitNumberValue = await sequences.NextValueAsync(SequenceKeys.MhcVisitNumber, branchId, year, ct);
        var visitNumber = $"MHC-{year}-{visitNumberValue.ToString().PadLeft(6, '0')}";

        var tokenScope = EncodeDate(today);
        var tokenNumber = (int)await sequences.NextValueAsync(SequenceKeys.MhcVisitToken, request.MhcDoctorId, tokenScope, ct);

        var visit = new OpVisit
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            Patient = patient,
            Category = VisitCategory.Mhc,
            MhcDoctorId = request.MhcDoctorId,
            MhcDoctor = mhcDoctor,
            MhcPackageId = request.MhcPackageId,
            MhcPackage = package,
            VisitNumber = visitNumber,
            VisitDate = today,
            TokenNumber = tokenNumber,
            VisitType = visitType,
            ConsultationFee = package.Price,
        };
        db.OpVisits.Add(visit);
        await db.SaveChangesAsync(ct);

        return ToDetailDto(visit);
    }

    /// <summary>
    /// "Camp Bulk Import" — one camp/doctor picked for the whole batch, the CSV supplies only
    /// the varying per-row UHID. Delegates each row to RegisterAsync so every existing
    /// validation/business rule (blocked patient, fee calc, review-vs-new, token numbering)
    /// applies unchanged; a bad row is recorded as an error and the import continues, same
    /// forgiving-per-row posture as MASTER.ICD.IMPORT.
    /// </summary>
    public async Task<BulkImportResultDto> ImportCampVisitsAsync(
        long campId, long doctorId, Stream csv, CancellationToken ct = default)
    {
        if (!await db.Camps.AnyAsync(c => c.Id == campId, ct))
        {
            throw new NotFoundException(nameof(Camp), campId);
        }
        if (!await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), doctorId);
        }

        var (rows, errors) = ParseUhidCsv(csv);
        var inserted = 0;
        foreach (var (lineNo, uhid) in rows)
        {
            var patientId = await db.Patients.AsNoTracking()
                .Where(p => p.Uhid == uhid).Select(p => (long?)p.Id).FirstOrDefaultAsync(ct);
            if (patientId is null)
            {
                errors.Add($"Line {lineNo}: no patient found with UHID '{uhid}'.");
                continue;
            }
            try
            {
                await RegisterAsync(new RegisterOpVisitRequest(patientId.Value, doctorId, null, campId), ct);
                inserted++;
            }
            catch (Exception ex) when (ex is DomainException or NotFoundException)
            {
                errors.Add($"Line {lineNo}: UHID '{uhid}' — {ex.Message}");
            }
        }

        return new BulkImportResultDto(inserted, errors.Count, errors);
    }

    /// <summary>"MHC Bulk Import" — same batch-level-doctor/package + per-row-UHID design as ImportCampVisitsAsync, delegating each row to RegisterMhcAsync.</summary>
    public async Task<BulkImportResultDto> ImportMhcVisitsAsync(
        long mhcDoctorId, long mhcPackageId, Stream csv, CancellationToken ct = default)
    {
        if (!await db.MhcDoctors.AnyAsync(d => d.Id == mhcDoctorId, ct))
        {
            throw new NotFoundException(nameof(MhcDoctor), mhcDoctorId);
        }
        if (!await db.MhcPackages.AnyAsync(p => p.Id == mhcPackageId, ct))
        {
            throw new NotFoundException(nameof(MhcPackage), mhcPackageId);
        }

        var (rows, errors) = ParseUhidCsv(csv);
        var inserted = 0;
        foreach (var (lineNo, uhid) in rows)
        {
            var patientId = await db.Patients.AsNoTracking()
                .Where(p => p.Uhid == uhid).Select(p => (long?)p.Id).FirstOrDefaultAsync(ct);
            if (patientId is null)
            {
                errors.Add($"Line {lineNo}: no patient found with UHID '{uhid}'.");
                continue;
            }
            try
            {
                await RegisterMhcAsync(new RegisterMhcVisitRequest(patientId.Value, mhcDoctorId, mhcPackageId), ct);
                inserted++;
            }
            catch (Exception ex) when (ex is DomainException or NotFoundException)
            {
                errors.Add($"Line {lineNo}: UHID '{uhid}' — {ex.Message}");
            }
        }

        return new BulkImportResultDto(inserted, errors.Count, errors);
    }

    /// <summary>Single-column CSV: UHID per line, optional header row ("uhid" literal), blank lines skipped, in-file duplicates flagged as errors rather than double-registered.</summary>
    private static (List<(int LineNo, string Uhid)> Rows, List<string> Errors) ParseUhidCsv(Stream csv)
    {
        var rows = new List<(int, string)>();
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(csv);
        var lineNo = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNo++;
            var uhid = line.Split(',')[0].Trim();
            if (uhid.Length == 0)
            {
                continue;
            }
            if (lineNo == 1 && uhid.Equals("uhid", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!seen.Add(uhid))
            {
                errors.Add($"Line {lineNo}: duplicate UHID '{uhid}' in file, skipped.");
                continue;
            }
            rows.Add((lineNo, uhid));
        }
        return (rows, errors);
    }

    public async Task<OpVisitDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.MhcDoctor)
            .Include(v => v.Camp)
            .Include(v => v.Department)
            .Include(v => v.VisitClass)
            .Include(v => v.Session)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);
        return ToDetailDto(visit);
    }

    public async Task UpdateCampAsync(long id, UpdateVisitCampRequest request, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);
        if (visit.Status != OpVisitStatus.Waiting)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot be modified.");
        }
        if (request.CampId is { } campId && !await db.Camps.AnyAsync(c => c.Id == campId, ct))
        {
            throw new NotFoundException(nameof(Camp), campId);
        }

        visit.CampId = request.CampId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<QueueEntryDto>> GetQueueAsync(long? doctorId, DateOnly date, CancellationToken ct = default)
    {
        // MHC visits (no consultation workspace in this pass) never appear in the doctor queue.
        var visits = db.OpVisits.AsNoTracking()
            .Where(v => v.Category == VisitCategory.Opd && v.VisitDate == date &&
                (v.Status == OpVisitStatus.Waiting || v.Status == OpVisitStatus.InConsult));
        if (doctorId is { } id) visits = visits.Where(v => v.DoctorId == id);

        return await visits
            .OrderBy(v => v.DoctorId).ThenBy(v => v.TokenNumber)
            .Select(v => new QueueEntryDto(
                v.Id, v.TokenNumber, v.PatientId, v.Patient!.FirstName + " " + v.Patient!.LastName, v.Patient!.Uhid,
                v.DoctorId!.Value, v.Doctor!.User!.FullName, v.VisitType, v.Status))
            .ToListAsync(ct);
    }

    public async Task StartConsultAsync(long id, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.Include(v => v.Doctor).ThenInclude(d => d!.User).FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);
        await EnsureOwnVisitAsync(visit.DoctorId, visit.Doctor?.User?.FullName, ct);
        if (visit.Status != OpVisitStatus.Waiting)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot start consultation.");
        }
        visit.Status = OpVisitStatus.InConsult;
        visit.InConsultAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.Include(v => v.Doctor).ThenInclude(d => d!.User).FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);
        await EnsureOwnVisitAsync(visit.DoctorId, visit.Doctor?.User?.FullName, ct);
        if (visit.Status != OpVisitStatus.InConsult)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot be completed. Start the consultation first.");
        }
        visit.Status = OpVisitStatus.Completed;
        visit.CompletedAt = timeProvider.GetUtcNow();

        if (visit.AppointmentId is { } appointmentId)
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
            if (appointment is { Status: AppointmentStatus.CheckedIn })
            {
                appointment.Status = AppointmentStatus.Completed;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long id, CancelVisitRequest request, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.Include(v => v.Patient).FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);
        if (visit.Status is OpVisitStatus.Completed or OpVisitStatus.Cancelled)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot be cancelled.");
        }

        visit.Status = OpVisitStatus.Cancelled;
        visit.CancelReason = request.Reason;
        visit.CancelledAt = timeProvider.GetUtcNow();

        var cancelEvent = visit.Category == VisitCategory.Opd ? ActivityEvents.OpVisitCancelled : ActivityEvents.MhcVisitCancelled;
        await activity.LogAsync(cancelEvent,
            $"Visit {visit.VisitNumber} for '{visit.Patient!.FirstName} {visit.Patient!.LastName}' cancelled: {request.Reason}",
            ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ConsultationWorkspaceDto> GetWorkspaceAsync(long id, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.MhcDoctor)
            .Include(v => v.Camp)
            .Include(v => v.Department)
            .Include(v => v.VisitClass)
            .Include(v => v.Session)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException(nameof(OpVisit), id);

        var recentVisitRows = await db.OpVisits.AsNoTracking()
            .Where(v => v.PatientId == visit.PatientId && v.Id != id && v.Status == OpVisitStatus.Completed)
            .OrderByDescending(v => v.VisitDate)
            .Take(5)
            .Select(v => new { v.Id, v.VisitDate, DoctorName = v.Doctor!.User!.FullName })
            .ToListAsync(ct);
        var recentVisitIds = recentVisitRows.Select(v => v.Id).ToList();
        var diagnosesByVisit = await db.VisitDiagnoses.AsNoTracking()
            .Where(d => recentVisitIds.Contains(d.VisitId))
            .Select(d => new { d.VisitId, d.IcdCode!.Description })
            .ToListAsync(ct);
        var recentVisits = recentVisitRows.Select(v => new RecentVisitSummaryDto(
            v.Id, v.VisitDate, v.DoctorName,
            diagnosesByVisit.Where(d => d.VisitId == v.Id).Select(d => d.Description).ToList())).ToList();

        var medsCutoff = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-90);
        var recentMedications = await db.PrescriptionItems.AsNoTracking()
            .Where(pi => pi.Prescription!.Visit!.PatientId == visit.PatientId && pi.Prescription!.Visit!.VisitDate >= medsCutoff)
            .OrderByDescending(pi => pi.Prescription!.Visit!.VisitDate)
            .Take(20)
            .Select(pi => new RecentMedicationDto(pi.Drug!.Name, pi.Dose, pi.Frequency, pi.Prescription!.Visit!.VisitDate))
            .ToListAsync(ct);

        var vitalsEntity = await db.VisitVitals.AsNoTracking().FirstOrDefaultAsync(v => v.VisitId == id, ct);
        var vitals = vitalsEntity is null ? null : new VisitVitalsDto(
            vitalsEntity.Id, vitalsEntity.HeightCm, vitalsEntity.WeightKg, vitalsEntity.Bmi, vitalsEntity.TemperatureF,
            vitalsEntity.PulseRate, vitalsEntity.RespiratoryRate, vitalsEntity.BloodPressureSystolic,
            vitalsEntity.BloodPressureDiastolic, vitalsEntity.SpO2, vitalsEntity.RandomBloodSugar);

        var complaints = await db.VisitComplaints.AsNoTracking().Where(c => c.VisitId == id)
            .Select(c => new VisitComplaintDto(c.Id, c.Complaint, c.Duration))
            .ToListAsync(ct);

        var examinationEntity = await db.VisitExaminations.AsNoTracking().FirstOrDefaultAsync(e => e.VisitId == id, ct);
        var examination = examinationEntity is null
            ? null
            : new VisitExaminationDto(examinationEntity.HistoryNotes, examinationEntity.ExaminationNotes);

        var diagnoses = await db.VisitDiagnoses.AsNoTracking().Where(d => d.VisitId == id)
            .Select(d => new VisitDiagnosisDto(d.Id, d.IcdCodeId, d.IcdCode!.Code, d.IcdCode!.Description, d.Type, d.Notes))
            .ToListAsync(ct);

        var prescriptionEntity = await db.Prescriptions.AsNoTracking().FirstOrDefaultAsync(p => p.VisitId == id, ct);
        PrescriptionDto? prescription = null;
        if (prescriptionEntity is not null)
        {
            var items = await db.PrescriptionItems.AsNoTracking().Where(i => i.PrescriptionId == prescriptionEntity.Id)
                .Select(i => new PrescriptionItemDto(i.Id, i.DrugId, i.Drug!.Name, i.Dose, i.Frequency, i.Duration, i.Route, i.Instructions))
                .ToListAsync(ct);
            prescription = new PrescriptionDto(prescriptionEntity.Id, prescriptionEntity.Notes, items);
        }

        var orders = await db.ClinicalOrders.AsNoTracking().Where(o => o.VisitId == id)
            .Select(o => new ClinicalOrderDto(o.Id, o.OrderType, o.Description, o.Priority, o.Status, o.Notes))
            .ToListAsync(ct);

        var referrals = await db.VisitReferrals.AsNoTracking().Where(r => r.VisitId == id)
            .Select(r => new VisitReferralDto(
                r.Id, r.Direction, r.ReferredToDoctorId,
                r.ReferredToDoctor != null ? r.ReferredToDoctor.User!.FullName : null, r.ExternalDetails, r.Reason,
                r.OpinionNotes, r.OpinionEnteredAt))
            .ToListAsync(ct);

        var followUpEntity = await db.FollowUpAdvices.AsNoTracking().FirstOrDefaultAsync(f => f.VisitId == id, ct);
        var followUp = followUpEntity is null
            ? null
            : new FollowUpAdviceDto(followUpEntity.Id, followUpEntity.FollowUpDate, followUpEntity.Notes);

        return new ConsultationWorkspaceDto(
            ToDetailDto(visit), recentVisits, recentMedications, vitals, complaints, examination, diagnoses,
            prescription, orders, referrals, followUp);
    }

    public async Task SaveVitalsAsync(long visitId, SaveVitalsRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var vitals = await db.VisitVitals.FirstOrDefaultAsync(v => v.VisitId == visitId, ct);
        if (vitals is null)
        {
            vitals = new VisitVitals { VisitId = visitId };
            db.VisitVitals.Add(vitals);
        }

        vitals.HeightCm = request.HeightCm;
        vitals.WeightKg = request.WeightKg;
        vitals.Bmi = ComputeBmi(request.HeightCm, request.WeightKg);
        vitals.TemperatureF = request.TemperatureF;
        vitals.PulseRate = request.PulseRate;
        vitals.RespiratoryRate = request.RespiratoryRate;
        vitals.BloodPressureSystolic = request.BloodPressureSystolic;
        vitals.BloodPressureDiastolic = request.BloodPressureDiastolic;
        vitals.SpO2 = request.SpO2;
        vitals.RandomBloodSugar = request.RandomBloodSugar;
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveComplaintsAsync(long visitId, SaveComplaintsRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var existing = await db.VisitComplaints.Where(c => c.VisitId == visitId).ToListAsync(ct);
        db.VisitComplaints.RemoveRange(existing);
        foreach (var complaint in request.Complaints)
        {
            db.VisitComplaints.Add(new VisitComplaint { VisitId = visitId, Complaint = complaint.Complaint, Duration = complaint.Duration });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveExaminationAsync(long visitId, SaveExaminationRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var examination = await db.VisitExaminations.FirstOrDefaultAsync(e => e.VisitId == visitId, ct);
        if (examination is null)
        {
            examination = new VisitExamination { VisitId = visitId };
            db.VisitExaminations.Add(examination);
        }
        examination.HistoryNotes = request.HistoryNotes;
        examination.ExaminationNotes = request.ExaminationNotes;
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveDiagnosesAsync(long visitId, SaveDiagnosesRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var icdIds = request.Diagnoses.Select(d => d.IcdCodeId).Distinct().ToList();
        var validCount = await db.IcdCodes.CountAsync(i => icdIds.Contains(i.Id), ct);
        if (validCount != icdIds.Count)
        {
            throw new DomainException("One or more ICD codes do not exist.");
        }

        var existing = await db.VisitDiagnoses.Where(d => d.VisitId == visitId).ToListAsync(ct);
        db.VisitDiagnoses.RemoveRange(existing);
        foreach (var diagnosis in request.Diagnoses)
        {
            db.VisitDiagnoses.Add(new VisitDiagnosis
            {
                VisitId = visitId, IcdCodeId = diagnosis.IcdCodeId, Type = diagnosis.Type, Notes = diagnosis.Notes,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SavePrescriptionAsync(long visitId, SavePrescriptionRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var drugIds = request.Items.Select(i => i.DrugId).Distinct().ToList();
        var validCount = await db.Drugs.CountAsync(d => drugIds.Contains(d.Id), ct);
        if (validCount != drugIds.Count)
        {
            throw new DomainException("One or more drugs do not exist.");
        }

        var prescription = await db.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.VisitId == visitId, ct);
        if (prescription is null)
        {
            prescription = new Prescription { VisitId = visitId };
            db.Prescriptions.Add(prescription);
        }
        prescription.Notes = request.Notes;

        db.PrescriptionItems.RemoveRange(prescription.Items);
        prescription.Items = request.Items.Select(i => new PrescriptionItem
        {
            DrugId = i.DrugId, Dose = i.Dose, Frequency = i.Frequency, Duration = i.Duration,
            Route = i.Route, Instructions = i.Instructions,
        }).ToList();

        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddOrderAsync(long visitId, AddClinicalOrderRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var order = new ClinicalOrder
        {
            VisitId = visitId, OrderType = request.OrderType, Description = request.Description,
            Priority = request.Priority, Notes = request.Notes,
        };
        db.ClinicalOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return order.Id;
    }

    public async Task<IReadOnlyList<ClinicalOrderDto>> GetOrdersAsync(long visitId, CancellationToken ct = default) =>
        await db.ClinicalOrders.AsNoTracking().Where(o => o.VisitId == visitId)
            .Select(o => new ClinicalOrderDto(o.Id, o.OrderType, o.Description, o.Priority, o.Status, o.Notes))
            .ToListAsync(ct);

    public async Task CancelOrderAsync(long visitId, long orderId, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var order = await db.ClinicalOrders.FirstOrDefaultAsync(o => o.Id == orderId && o.VisitId == visitId, ct)
            ?? throw new NotFoundException(nameof(ClinicalOrder), orderId);
        if (order.Status is ClinicalOrderStatus.Completed or ClinicalOrderStatus.Cancelled)
        {
            throw new DomainException($"An order that is {order.Status} cannot be cancelled.");
        }
        order.Status = ClinicalOrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddReferralAsync(long visitId, AddVisitReferralRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        if (request.Direction == ReferralDirection.Internal && request.ReferredToDoctorId is { } doctorId
            && !await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new DomainException("Referred-to doctor does not exist.");
        }

        var referral = new VisitReferral
        {
            VisitId = visitId,
            Direction = request.Direction,
            ReferredToDoctorId = request.Direction == ReferralDirection.Internal ? request.ReferredToDoctorId : null,
            ExternalDetails = request.Direction == ReferralDirection.External ? request.ExternalDetails : null,
            Reason = request.Reason,
        };
        db.VisitReferrals.Add(referral);
        await db.SaveChangesAsync(ct);
        return referral.Id;
    }

    public async Task<IReadOnlyList<VisitReferralDto>> GetReferralsAsync(long visitId, CancellationToken ct = default) =>
        await db.VisitReferrals.AsNoTracking().Where(r => r.VisitId == visitId)
            .Select(r => new VisitReferralDto(
                r.Id, r.Direction, r.ReferredToDoctorId,
                r.ReferredToDoctor != null ? r.ReferredToDoctor.User!.FullName : null, r.ExternalDetails, r.Reason,
                r.OpinionNotes, r.OpinionEnteredAt))
            .ToListAsync(ct);

    public async Task DeleteReferralAsync(long visitId, long referralId, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var referral = await db.VisitReferrals.FirstOrDefaultAsync(r => r.Id == referralId && r.VisitId == visitId, ct)
            ?? throw new NotFoundException(nameof(VisitReferral), referralId);
        db.VisitReferrals.Remove(referral);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>"Doctor Opinion Entry" — records the referred-to doctor's cross-consult response.
    /// No caller-identity check here (a staff member may transcribe a verbal opinion); the Doctor
    /// Portal wrapper (DoctorPortalService.SaveMyOpinionAsync) adds the ownership check for the
    /// self-service path.</summary>
    public async Task SaveOpinionAsync(long visitId, long referralId, SaveReferralOpinionRequest request, CancellationToken ct = default)
    {
        var referral = await db.VisitReferrals.FirstOrDefaultAsync(r => r.Id == referralId && r.VisitId == visitId, ct)
            ?? throw new NotFoundException(nameof(VisitReferral), referralId);
        if (referral.Direction != ReferralDirection.Internal)
        {
            throw new DomainException("Only an internal (cross-consult) referral can have an opinion recorded.");
        }

        referral.OpinionNotes = request.OpinionNotes;
        referral.OpinionEnteredAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A doctor's cross-consult inbox — every Internal referral addressed to them, pending
    /// ones first (oldest first within each group), across all patients/visits.</summary>
    public async Task<IReadOnlyList<OpinionRequestDto>> GetOpinionRequestsForDoctorAsync(long doctorId, CancellationToken ct = default) =>
        await db.VisitReferrals.AsNoTracking()
            .Where(r => r.Direction == ReferralDirection.Internal && r.ReferredToDoctorId == doctorId)
            .OrderBy(r => r.OpinionEnteredAt.HasValue).ThenBy(r => r.Id)
            .Select(r => new OpinionRequestDto(
                r.Id, r.VisitId, r.Visit!.VisitNumber, r.Visit.VisitDate,
                r.Visit.PatientId, r.Visit.Patient!.Uhid, r.Visit.Patient!.FirstName + " " + r.Visit.Patient!.LastName,
                r.Visit.Doctor != null ? r.Visit.Doctor.User!.FullName : "—", r.Reason, r.OpinionNotes, r.OpinionEnteredAt))
            .ToListAsync(ct);

    public async Task SaveFollowUpAsync(long visitId, SaveFollowUpRequest request, CancellationToken ct = default)
    {
        await EnsureConsultationWritableAsync(visitId, ct);
        var followUp = await db.FollowUpAdvices.FirstOrDefaultAsync(f => f.VisitId == visitId, ct);
        if (followUp is null)
        {
            followUp = new FollowUpAdvice { VisitId = visitId };
            db.FollowUpAdvices.Add(followUp);
        }
        followUp.FollowUpDate = request.FollowUpDate;
        followUp.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GenerateVisitSlipAsync(long visitId, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .Include(v => v.MhcDoctor)
            .FirstOrDefaultAsync(v => v.Id == visitId, ct)
            ?? throw new NotFoundException(nameof(OpVisit), visitId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == visit.BranchId, ct);
        var providerName = visit.Category == VisitCategory.Opd ? visit.Doctor!.User!.FullName : visit.MhcDoctor!.Name;

        var data = new VisitSlipData(
            visit.VisitNumber, visit.TokenNumber, FormatName(visit.Patient!), visit.Patient!.Uhid,
            providerName, branch?.Name ?? string.Empty, visit.VisitDate, visit.VisitType.ToString());
        return pdf.GenerateVisitSlip(data);
    }

    public async Task<byte[]> GenerateVisitSummaryAsync(long visitId, CancellationToken ct = default)
    {
        var visit = await db.OpVisits.AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(v => v.Id == visitId, ct)
            ?? throw new NotFoundException(nameof(OpVisit), visitId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == visit.BranchId, ct);

        var diagnoses = await db.VisitDiagnoses.AsNoTracking().Where(d => d.VisitId == visitId)
            .Select(d => d.IcdCode!.Code + " — " + d.IcdCode!.Description)
            .ToListAsync(ct);

        var prescriptionLines = await db.PrescriptionItems.AsNoTracking()
            .Where(i => i.Prescription!.VisitId == visitId)
            .Select(i => i.Drug!.Name + " — " + i.Dose + " — " + i.Frequency + " — " + i.Duration +
                (i.Instructions != null ? " (" + i.Instructions + ")" : ""))
            .ToListAsync(ct);

        var followUp = await db.FollowUpAdvices.AsNoTracking().FirstOrDefaultAsync(f => f.VisitId == visitId, ct);
        var examination = await db.VisitExaminations.AsNoTracking().FirstOrDefaultAsync(e => e.VisitId == visitId, ct);

        var data = new VisitSummaryData(
            visit.VisitNumber, FormatName(visit.Patient!), visit.Patient!.Uhid, visit.Doctor!.User!.FullName,
            branch?.Name ?? string.Empty, visit.VisitDate, diagnoses, prescriptionLines,
            followUp?.Notes ?? examination?.ExaminationNotes, followUp?.FollowUpDate);
        return pdf.GenerateVisitSummary(data);
    }

    /// <summary>
    /// Guards every consultation write: the visit must still be open (Waiting / InConsult) — a
    /// completed or cancelled consultation is a locked clinical record; corrections are recorded as
    /// an EMR clinical note (addendum) instead — and a doctor may only write into their own patient's
    /// consultation (staff without a doctor profile, e.g. a nurse taking vitals, are not restricted).
    /// </summary>
    private async Task EnsureConsultationWritableAsync(long visitId, CancellationToken ct)
    {
        var visit = await db.OpVisits.AsNoTracking().Where(v => v.Id == visitId)
            .Select(v => new { v.Status, v.DoctorId, DoctorName = v.Doctor != null ? v.Doctor.User!.FullName : null })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(OpVisit), visitId);
        if (visit.Status is OpVisitStatus.Completed or OpVisitStatus.Cancelled)
        {
            throw new DomainException(
                $"This consultation is {visit.Status} and locked. Record any correction as a clinical note (addendum) in the patient's EMR.");
        }
        await EnsureOwnVisitAsync(visit.DoctorId, visit.DoctorName, ct);
    }

    private async Task EnsureOwnVisitAsync(long? visitDoctorId, string? visitDoctorName, CancellationToken ct)
    {
        var callerDoctorId = await Prixo.Application.Common.Security.ClinicalActor.GetCallerDoctorIdAsync(db, currentUser, ct);
        if (callerDoctorId is { } mine && visitDoctorId is { } owner && mine != owner)
        {
            throw new ForbiddenException($"This consultation belongs to {visitDoctorName ?? "another doctor"}. Use a referral to involve another doctor.");
        }
    }

    private async Task EnsureVisitExistsAsync(long visitId, CancellationToken ct)
    {
        if (!await db.OpVisits.AnyAsync(v => v.Id == visitId, ct))
        {
            throw new NotFoundException(nameof(OpVisit), visitId);
        }
    }

    private OpVisitDetailDto ToDetailDto(OpVisit v)
    {
        var now = timeProvider.GetUtcNow();
        var providerId = v.Category == VisitCategory.Opd ? v.DoctorId!.Value : v.MhcDoctorId!.Value;
        var providerName = v.Category == VisitCategory.Opd ? v.Doctor!.User!.FullName : v.MhcDoctor!.Name;
        return new OpVisitDetailDto(
            v.Id, v.VisitNumber, v.TokenNumber, v.PatientId, v.Patient!.Uhid, FormatName(v.Patient!),
            v.Patient!.Mobile, v.Patient!.Gender.ToString(), FormatAge(v.Patient!.DateOfBirth, v.Patient!.EstimatedAge, now),
            providerId, providerName, v.AppointmentId, v.VisitDate, v.VisitType, v.Status, v.ConsultationFee,
            v.InConsultAt, v.CompletedAt, v.CancelReason, v.CancelledAt, v.CampId, v.Camp?.Name,
            v.BookingType, v.VisitClassId, v.VisitClass?.Name, v.SessionId, v.Session?.Name, v.IsMlcCase,
            v.DepartmentId, v.Department?.Name);
    }

    private static decimal? ComputeBmi(decimal? heightCm, decimal? weightKg)
    {
        if (heightCm is not > 0 || weightKg is not > 0)
        {
            return null;
        }
        var heightM = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (heightM * heightM), 1);
    }

    private static int EncodeDate(DateOnly date) => (date.Year * 10000) + (date.Month * 100) + date.Day;

    private static string FormatName(Patient patient) =>
        string.Join(' ', new[] { patient.FirstName, patient.MiddleName, patient.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string FormatAge(DateOnly? dateOfBirth, int? estimatedAge, DateTimeOffset now)
    {
        if (dateOfBirth is { } dob)
        {
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            var years = today.Year - dob.Year;
            if (dob > today.AddYears(-years))
            {
                years--;
            }
            if (years >= 2)
            {
                return $"{years} yr(s)";
            }
            var months = ((today.Year - dob.Year) * 12) + today.Month - dob.Month - (today.Day < dob.Day ? 1 : 0);
            return months <= 0 ? "< 1 month" : $"{months} month(s)";
        }
        return estimatedAge is { } age ? $"~{age} yr(s) (estimated)" : "Unknown";
    }
}
