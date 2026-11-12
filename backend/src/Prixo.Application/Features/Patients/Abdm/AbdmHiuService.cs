using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Patients.Abdm;

/// <summary>
/// ABDM Milestone M3 — Health Information User (HIU) service.
/// Allows healthcare practitioners to raise consent requests to patients, track approvals,
/// and securely fetch/view longitudinal external health records (consultation notes, diagnostic reports,
/// prescriptions, discharge summaries) from across India's ABDM health data network.
/// </summary>
public class AbdmHiuService(
    IAppDbContext db,
    IAbdmGateway gateway,
    SettingService settings,
    ICurrentUser currentUser,
    IActivityLogger activity,
    TimeProvider clock)
{
    public async Task<ConsentRequestDto> InitiateConsentRequestAsync(
        InitiateConsentRequestDto request, CancellationToken ct = default)
    {
        await RequireM3ActiveAsync(ct);

        var patient = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        var abha = await db.AbhaProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == request.PatientId, ct)
            ?? throw new DomainException("Patient does not have a linked ABHA. Milestone 1 (ABHA identity) is required before raising an HIU consent request.");

        var hiTypesStr = string.Join(",", request.HiTypes);

        var reqParams = new AbdmConsentRequestParams(
            abha.AbhaNumber,
            abha.AbhaAddress,
            request.Purpose,
            request.HiTypes,
            request.DateFrom,
            request.DateTo,
            request.ExpiryAt,
            currentUser.UserName ?? "Dr. Clinician");

        var consentReqId = await gateway.SendConsentRequestAsync(reqParams, ct);

        var consentReq = new AbdmConsentRequest
        {
            PatientId = request.PatientId,
            ConsentRequestId = consentReqId,
            AbhaNumber = abha.AbhaNumber,
            AbhaAddress = abha.AbhaAddress,
            Purpose = request.Purpose,
            HiTypes = hiTypesStr,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            ExpiryAt = request.ExpiryAt,
            Status = AbdmConsentStatus.Requested,
            RequesterDoctorId = currentUser.UserId > 0 ? currentUser.UserId : null,
            RequesterDoctorName = currentUser.UserName,
        };

        db.AbdmConsentRequests.Add(consentReq);
        await db.SaveChangesAsync(ct);

        await activity.LogAsync(
            "ABDM_CONSENT_REQUEST_INITIATED",
            $"Initiated ABDM HIU consent request {consentReqId} for patient #{request.PatientId} ({abha.AbhaNumber})", ct: ct);

        return MapConsent(consentReq);
    }

    public async Task<IReadOnlyList<ConsentRequestDto>> GetConsentRequestsAsync(
        long patientId, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);

        return await db.AbdmConsentRequests.AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapConsent(r))
            .ToListAsync(ct);
    }

    public async Task<ConsentRequestDto> CheckConsentStatusAsync(
        long consentRequestId, CancellationToken ct = default)
    {
        await RequireM3ActiveAsync(ct);

        var req = await db.AbdmConsentRequests
            .FirstOrDefaultAsync(r => r.Id == consentRequestId, ct)
            ?? throw new NotFoundException(nameof(AbdmConsentRequest), consentRequestId);

        var check = await gateway.CheckConsentStatusAsync(req.ConsentRequestId, ct);

        req.Status = check.Status;
        if (!string.IsNullOrWhiteSpace(check.ConsentArtifactId))
        {
            req.ConsentArtifactId = check.ConsentArtifactId;
        }
        req.StatusUpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        await activity.LogAsync(
            "ABDM_CONSENT_STATUS_CHECKED",
            $"Consent request {req.ConsentRequestId} updated to {req.Status}", ct: ct);

        return MapConsent(req);
    }

    public async Task<IReadOnlyList<ExternalHealthRecordDto>> FetchExternalHealthRecordsAsync(
        long consentRequestId, CancellationToken ct = default)
    {
        await RequireM3ActiveAsync(ct);

        var req = await db.AbdmConsentRequests
            .FirstOrDefaultAsync(r => r.Id == consentRequestId, ct)
            ?? throw new NotFoundException(nameof(AbdmConsentRequest), consentRequestId);

        if (req.Status != AbdmConsentStatus.Granted || string.IsNullOrWhiteSpace(req.ConsentArtifactId))
        {
            // If still requested, automatically check/grant in sandbox
            var statusResult = await gateway.CheckConsentStatusAsync(req.ConsentRequestId, ct);
            req.Status = statusResult.Status;
            req.ConsentArtifactId = statusResult.ConsentArtifactId;
            req.StatusUpdatedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }

        if (req.Status != AbdmConsentStatus.Granted || string.IsNullOrWhiteSpace(req.ConsentArtifactId))
        {
            throw new DomainException("Cannot fetch records: Consent request has not been granted by the patient.");
        }

        var items = await gateway.FetchExternalHealthDataAsync(req.ConsentArtifactId, ct);

        var newRecords = new List<AbdmExternalHealthRecord>();
        foreach (var item in items)
        {
            newRecords.Add(new AbdmExternalHealthRecord
            {
                PatientId = req.PatientId,
                ConsentRequestId = req.Id,
                HiType = item.HiType,
                SourceFacilityName = item.SourceFacilityName,
                SourceFacilityHipId = item.SourceFacilityHipId,
                RecordDate = item.RecordDate,
                DoctorName = item.DoctorName,
                Title = item.Title,
                ClinicalSummary = item.ClinicalSummary,
                FhirBundleJson = item.FhirBundleJson,
            });
        }

        db.AbdmExternalHealthRecords.AddRange(newRecords);
        await db.SaveChangesAsync(ct);

        await activity.LogAsync(
            "ABDM_EXTERNAL_RECORDS_FETCHED",
            $"Retrieved {items.Count} external health records via ABDM consent {req.ConsentArtifactId} for patient #{req.PatientId}", ct: ct);

        return await GetExternalHealthRecordsAsync(req.PatientId, ct);
    }

    public async Task<IReadOnlyList<ExternalHealthRecordDto>> GetExternalHealthRecordsAsync(
        long patientId, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);

        return await db.AbdmExternalHealthRecords.AsNoTracking()
            .Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.RecordDate)
            .Select(h => new ExternalHealthRecordDto(
                h.Id,
                h.PatientId,
                h.ConsentRequestId,
                h.HiType,
                h.SourceFacilityName,
                h.SourceFacilityHipId,
                h.RecordDate,
                h.DoctorName,
                h.Title,
                h.ClinicalSummary,
                h.FhirBundleJson,
                h.CreatedAt))
            .ToListAsync(ct);
    }

    private async Task RequireM3ActiveAsync(CancellationToken ct)
    {
        var master = await settings.GetBoolAsync(SettingKeys.AbdmEnabled, true, ct);
        if (!master)
        {
            throw new DomainException("ABDM integration is currently disabled in system settings.");
        }
        var m3 = await settings.GetBoolAsync(SettingKeys.AbdmM3Enabled, true, ct);
        if (!m3)
        {
            throw new DomainException("ABDM Milestone 3 (HIU - Health Information User) is currently disabled in system settings.");
        }
    }

    private async Task EnsurePatientExistsAsync(long patientId, CancellationToken ct)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
    }

    private static ConsentRequestDto MapConsent(AbdmConsentRequest r) => new(
        r.Id,
        r.PatientId,
        r.ConsentRequestId,
        r.ConsentArtifactId,
        r.AbhaNumber,
        r.AbhaAddress,
        r.Purpose,
        r.HiTypes,
        r.DateFrom,
        r.DateTo,
        r.ExpiryAt,
        r.Status,
        r.StatusUpdatedAt,
        r.RequesterDoctorName,
        r.CreatedAt);
}
