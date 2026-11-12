using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Emr;

/// <summary>
/// Medical/surgical/family/immunization histories and allergies (plan §11). Plain,
/// repeatable, patient-scoped entries — same append-only-plus-toggle posture as IPD's
/// ClinicalChartService/Nursing's NursingChartService; only Allergy gets a deactivate action
/// since safety data needs a correction path without deleting the record.
/// </summary>
public class HistoryService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> AddMedicalHistoryAsync(long patientId, AddMedicalHistoryRequest request, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var entry = new MedicalHistory
        {
            PatientId = patientId, Condition = request.Condition, DiagnosedDate = request.DiagnosedDate,
            Notes = request.Notes, RecordedByUserId = currentUser.UserId, RecordedAt = timeProvider.GetUtcNow(),
        };
        db.MedicalHistories.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<MedicalHistoryDto>> GetMedicalHistoryAsync(long patientId, CancellationToken ct = default) =>
        await db.MedicalHistories.AsNoTracking().Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.RecordedAt)
            .Join(db.Users.AsNoTracking(), h => h.RecordedByUserId, u => u.Id, (h, u) =>
                new MedicalHistoryDto(h.Id, h.Condition, h.DiagnosedDate, h.Notes, u.FullName, h.RecordedAt))
            .ToListAsync(ct);

    public async Task<long> AddSurgicalHistoryAsync(long patientId, AddSurgicalHistoryRequest request, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var entry = new SurgicalHistory
        {
            PatientId = patientId, ProcedureName = request.ProcedureName, ProcedureDate = request.ProcedureDate,
            Surgeon = request.Surgeon, Notes = request.Notes, RecordedByUserId = currentUser.UserId, RecordedAt = timeProvider.GetUtcNow(),
        };
        db.SurgicalHistories.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<SurgicalHistoryDto>> GetSurgicalHistoryAsync(long patientId, CancellationToken ct = default) =>
        await db.SurgicalHistories.AsNoTracking().Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.RecordedAt)
            .Join(db.Users.AsNoTracking(), h => h.RecordedByUserId, u => u.Id, (h, u) =>
                new SurgicalHistoryDto(h.Id, h.ProcedureName, h.ProcedureDate, h.Surgeon, h.Notes, u.FullName, h.RecordedAt))
            .ToListAsync(ct);

    public async Task<long> AddFamilyHistoryAsync(long patientId, AddFamilyHistoryRequest request, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var entry = new FamilyHistory
        {
            PatientId = patientId, Relation = request.Relation, Condition = request.Condition,
            Notes = request.Notes, RecordedByUserId = currentUser.UserId, RecordedAt = timeProvider.GetUtcNow(),
        };
        db.FamilyHistories.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<FamilyHistoryDto>> GetFamilyHistoryAsync(long patientId, CancellationToken ct = default) =>
        await db.FamilyHistories.AsNoTracking().Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.RecordedAt)
            .Join(db.Users.AsNoTracking(), h => h.RecordedByUserId, u => u.Id, (h, u) =>
                new FamilyHistoryDto(h.Id, h.Relation, h.Condition, h.Notes, u.FullName, h.RecordedAt))
            .ToListAsync(ct);

    public async Task<long> AddImmunizationAsync(long patientId, AddImmunizationRequest request, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var entry = new Immunization
        {
            PatientId = patientId, VaccineName = request.VaccineName, DoseNumber = request.DoseNumber,
            AdministeredDate = request.AdministeredDate, NextDueDate = request.NextDueDate,
            AdministeredBy = request.AdministeredBy, Notes = request.Notes,
            RecordedByUserId = currentUser.UserId, RecordedAt = timeProvider.GetUtcNow(),
        };
        db.Immunizations.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<ImmunizationDto>> GetImmunizationsAsync(long patientId, CancellationToken ct = default) =>
        await db.Immunizations.AsNoTracking().Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.AdministeredDate)
            .Join(db.Users.AsNoTracking(), i => i.RecordedByUserId, u => u.Id, (i, u) =>
                new ImmunizationDto(i.Id, i.VaccineName, i.DoseNumber, i.AdministeredDate, i.NextDueDate, i.AdministeredBy, i.Notes, u.FullName, i.RecordedAt))
            .ToListAsync(ct);

    public async Task<long> AddAllergyAsync(long patientId, AddAllergyRequest request, CancellationToken ct = default)
    {
        await RequirePatientAsync(patientId, ct);
        var entry = new Allergy
        {
            PatientId = patientId, AllergenType = request.AllergenType, AllergenName = request.AllergenName,
            Reaction = request.Reaction, Severity = request.Severity, RecordedByUserId = currentUser.UserId,
            RecordedAt = timeProvider.GetUtcNow(),
        };
        db.Allergies.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<AllergyDto>> GetAllergiesAsync(long patientId, bool activeOnly, CancellationToken ct = default)
    {
        var query = db.Allergies.AsNoTracking().Where(a => a.PatientId == patientId);
        if (activeOnly) query = query.Where(a => a.IsActive);
        return await query.OrderByDescending(a => a.RecordedAt)
            .Join(db.Users.AsNoTracking(), a => a.RecordedByUserId, u => u.Id, (a, u) =>
                new AllergyDto(a.Id, a.AllergenType, a.AllergenName, a.Reaction, a.Severity, a.IsActive, u.FullName, a.RecordedAt))
            .ToListAsync(ct);
    }

    public async Task DeactivateAllergyAsync(long patientId, long allergyId, CancellationToken ct = default)
    {
        var allergy = await db.Allergies.FirstOrDefaultAsync(a => a.Id == allergyId && a.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(Allergy), allergyId);
        if (!allergy.IsActive)
        {
            throw new DomainException("Allergy is already inactive.");
        }
        allergy.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Cross-checks candidate drug names against the patient's active drug allergies — the "prescription warning" hook (plan §11). Advisory only; callers decide how to surface it, nothing here blocks a prescription.</summary>
    public async Task<IReadOnlyList<AllergyMatchDto>> CheckDrugAllergiesAsync(long patientId, IReadOnlyList<string> drugNames, CancellationToken ct = default)
    {
        if (drugNames.Count == 0)
        {
            return [];
        }
        var activeDrugAllergies = await db.Allergies.AsNoTracking()
            .Where(a => a.PatientId == patientId && a.IsActive && a.AllergenType == AllergenType.Drug)
            .Select(a => new { a.Id, a.AllergenName, a.Reaction, a.Severity })
            .ToListAsync(ct);

        return [.. from drugName in drugNames
                   from allergy in activeDrugAllergies
                   where EmrMath.MatchesAllergen(drugName, allergy.AllergenName)
                   select new AllergyMatchDto(drugName, allergy.Id, allergy.AllergenName, allergy.Reaction, allergy.Severity)];
    }

    private async Task RequirePatientAsync(long patientId, CancellationToken ct)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
    }
}
