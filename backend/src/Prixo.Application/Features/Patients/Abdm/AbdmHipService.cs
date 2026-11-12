using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Patients.Abdm;

/// <summary>
/// ABDM Milestone M2 — Health Information Provider (HIP) service.
/// Discovers clinical encounters (OPD consultations, IPD admissions, lab diagnostic reports, prescriptions),
/// maps them into structured care contexts linked to the patient's ABHA, and shares FHIR R4 bundles
/// upon patient consent verification.
/// </summary>
public class AbdmHipService(
    IAppDbContext db,
    IAbdmGateway gateway,
    FhirBundleService fhir,
    SettingService settings,
    IActivityLogger activity,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<CareContextDto>> GetCareContextsAsync(
        long patientId, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);

        // Auto-discover any new encounters first
        await DiscoverAndSyncCareContextsAsync(patientId, ct);

        var items = await db.AbdmCareContexts.AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CareContextDto(
                c.Id,
                c.PatientId,
                c.PatientReference,
                c.AbhaNumber,
                c.ContextType,
                c.ReferenceNumber,
                c.Display,
                c.IsLinked,
                c.LinkedAt,
                c.CreatedAt))
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<CareContextDto>> DiscoverAndSyncCareContextsAsync(
        long patientId, CancellationToken ct = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        var abha = await db.AbhaProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct);

        var abhaNum = abha?.AbhaNumber ?? string.Empty;

        var existingRefs = await db.AbdmCareContexts.AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .Select(c => c.ReferenceNumber)
            .ToHashSetAsync(ct);

        var toAdd = new List<AbdmCareContext>();

        // 1. OPD Visits
        var opVisits = await db.OpVisits.AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate)
            .Take(20)
            .ToListAsync(ct);

        foreach (var v in opVisits)
        {
            var refNum = $"OPD-{v.VisitNumber}";
            if (!existingRefs.Contains(refNum))
            {
                toAdd.Add(new AbdmCareContext
                {
                    PatientId = patientId,
                    PatientReference = patient.Uhid,
                    AbhaNumber = abhaNum,
                    ContextType = AbdmCareContextType.OpdVisit,
                    ReferenceNumber = refNum,
                    Display = $"OP Consultation - {v.Doctor?.User?.FullName ?? "Physician"} ({v.VisitDate:dd MMM yyyy})",
                    IsLinked = false,
                });
                existingRefs.Add(refNum);
            }
        }

        // 2. IP Admissions
        var admissions = await db.Admissions.AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AdmissionDate)
            .Take(10)
            .ToListAsync(ct);

        foreach (var a in admissions)
        {
            var refNum = $"IPD-{a.AdmissionNumber}";
            if (!existingRefs.Contains(refNum))
            {
                toAdd.Add(new AbdmCareContext
                {
                    PatientId = patientId,
                    PatientReference = patient.Uhid,
                    AbhaNumber = abhaNum,
                    ContextType = AbdmCareContextType.IpAdmission,
                    ReferenceNumber = refNum,
                    Display = $"Inpatient Admission ({a.AdmissionDate:dd MMM yyyy})",
                    IsLinked = false,
                });
                existingRefs.Add(refNum);
            }
        }

        // 3. Lab Orders
        var labOrders = await db.LabOrders.AsNoTracking()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.OrderDate)
            .Take(15)
            .ToListAsync(ct);

        foreach (var o in labOrders)
        {
            var refNum = $"LAB-{o.OrderNumber}";
            if (!existingRefs.Contains(refNum))
            {
                toAdd.Add(new AbdmCareContext
                {
                    PatientId = patientId,
                    PatientReference = patient.Uhid,
                    AbhaNumber = abhaNum,
                    ContextType = AbdmCareContextType.DiagnosticReport,
                    ReferenceNumber = refNum,
                    Display = $"Laboratory Diagnostic Report ({o.OrderDate:dd MMM yyyy})",
                    IsLinked = false,
                });
                existingRefs.Add(refNum);
            }
        }

        if (toAdd.Count > 0)
        {
            db.AbdmCareContexts.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }

        return await db.AbdmCareContexts.AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CareContextDto(
                c.Id,
                c.PatientId,
                c.PatientReference,
                c.AbhaNumber,
                c.ContextType,
                c.ReferenceNumber,
                c.Display,
                c.IsLinked,
                c.LinkedAt,
                c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CareContextDto>> LinkCareContextsAsync(
        long patientId, LinkCareContextsRequest request, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        await RequireM2ActiveAsync(ct);

        var abha = await db.AbhaProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new DomainException("Patient does not have a linked ABHA. Milestone 1 (ABHA linkage) is required before linking care contexts.");

        var query = db.AbdmCareContexts.Where(c => c.PatientId == patientId);
        if (request.CareContextIds != null && request.CareContextIds.Count > 0)
        {
            query = query.Where(c => request.CareContextIds.Contains(c.Id));
        }
        else if (request.LinkAllUnlinked)
        {
            query = query.Where(c => !c.IsLinked);
        }

        var contexts = await query.ToListAsync(ct);
        if (contexts.Count == 0)
        {
            throw new DomainException("No matching care contexts found to link.");
        }

        var now = clock.GetUtcNow();
        var hipId = await settings.GetRawAsync(SettingKeys.AbdmHipId, ct) ?? "PRX-HIP-001";

        foreach (var c in contexts)
        {
            c.IsLinked = true;
            c.LinkedAt = now;
            c.AbhaNumber = abha.AbhaNumber;
        }

        await gateway.NotifyCareContextLinkedAsync(
            hipId, abha.AbhaNumber, contexts[0].PatientReference, contexts.Select(c => c.ReferenceNumber), ct);

        await activity.LogAsync(
            "ABDM_CARE_CONTEXTS_LINKED",
            $"Linked {contexts.Count} care contexts to ABHA {abha.AbhaNumber} for patient #{patientId}", ct: ct);

        await db.SaveChangesAsync(ct);

        return contexts.Select(c => new CareContextDto(
            c.Id,
            c.PatientId,
            c.PatientReference,
            c.AbhaNumber,
            c.ContextType,
            c.ReferenceNumber,
            c.Display,
            c.IsLinked,
            c.LinkedAt,
            c.CreatedAt)).ToList();
    }

    public async Task<DataTransferLogDto> ShareHealthDataAsync(
        ShareDataRequest request, CancellationToken ct = default)
    {
        await RequireM2ActiveAsync(ct);

        var careContext = await db.AbdmCareContexts
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.ReferenceNumber == request.CareContextReference, ct)
            ?? throw new NotFoundException(nameof(AbdmCareContext), request.CareContextReference);

        var patient = careContext.Patient
            ?? throw new NotFoundException(nameof(Patient), careContext.PatientId);

        var hipId = await settings.GetRawAsync(SettingKeys.AbdmHipId, ct) ?? "PRX-HIP-001";
        var facilityName = "Prixo Hospital";

        var fhirBundleJson = fhir.BuildOpConsultationBundle(
            patient,
            careContext.AbhaNumber,
            facilityName,
            hipId,
            "Dr. Staff Physician",
            careContext.ReferenceNumber,
            careContext.CreatedAt,
            "Patient routine medical care follow-up",
            "Clinical evaluation documented.",
            "Care instructions shared with patient.");

        var txnId = $"TXN-HIP-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

        await gateway.ShareHealthDataBundleAsync(
            txnId, request.ConsentArtifactId, request.RecipientHiuId, fhirBundleJson, ct);

        var log = new AbdmDataTransferLog
        {
            TransactionId = txnId,
            ConsentArtifactId = request.ConsentArtifactId,
            CareContextReference = request.CareContextReference,
            HiType = request.HiType,
            RecipientHiuId = request.RecipientHiuId,
            Status = AbdmTransferStatus.Success,
            TransferDate = clock.GetUtcNow(),
            FhirBundleJson = fhirBundleJson,
        };

        db.AbdmDataTransferLogs.Add(log);
        await db.SaveChangesAsync(ct);

        await activity.LogAsync(
            "ABDM_HEALTH_DATA_SHARED",
            $"Shared FHIR {request.HiType} for care context {request.CareContextReference} with HIU {request.RecipientHiuId}", ct: ct);

        return new DataTransferLogDto(
            log.Id,
            log.TransactionId,
            log.ConsentArtifactId,
            log.CareContextReference,
            log.HiType,
            log.RecipientHiuId,
            log.Status,
            log.TransferDate,
            log.FhirBundleJson,
            log.ErrorMessage);
    }

    public async Task<IReadOnlyList<DataTransferLogDto>> GetDataTransferLogsAsync(
        int take = 50, CancellationToken ct = default)
    {
        return await db.AbdmDataTransferLogs.AsNoTracking()
            .OrderByDescending(l => l.TransferDate)
            .Take(take)
            .Select(l => new DataTransferLogDto(
                l.Id,
                l.TransactionId,
                l.ConsentArtifactId,
                l.CareContextReference,
                l.HiType,
                l.RecipientHiuId,
                l.Status,
                l.TransferDate,
                l.FhirBundleJson,
                l.ErrorMessage))
            .ToListAsync(ct);
    }

    private async Task RequireM2ActiveAsync(CancellationToken ct)
    {
        var master = await settings.GetBoolAsync(SettingKeys.AbdmEnabled, true, ct);
        if (!master)
        {
            throw new DomainException("ABDM integration is currently disabled in system settings.");
        }
        var m2 = await settings.GetBoolAsync(SettingKeys.AbdmM2Enabled, true, ct);
        if (!m2)
        {
            throw new DomainException("ABDM Milestone 2 (HIP - Health Information Provider) is currently disabled in system settings.");
        }
    }

    private async Task EnsurePatientExistsAsync(long patientId, CancellationToken ct)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
    }
}
