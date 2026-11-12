using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Patients.Abdm;

/// <summary>
/// ABDM Milestone M1 — link, create, or scan-and-share a patient's ABHA (Ayushman Bharat Health Account).
/// Enforces administrator enable/disable policies configured via System Settings.
/// Keeps the generic <see cref="PatientIdentifier"/> (Type = Abha) in sync.
/// All mutations are transactional and audited.
/// </summary>
public class AbdmService(
    IAppDbContext db,
    IAbdmGateway gateway,
    IUnitOfWork uow,
    IActivityLogger activity,
    SettingService settings,
    TimeProvider clock)
{
    private static readonly TimeSpan OtpSessionLifetime = TimeSpan.FromMinutes(10);

    public async Task<AbhaProfileDto?> GetProfileAsync(long patientId, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        var profile = await db.AbhaProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, ct);
        return profile is null ? null : Map(profile);
    }

    public async Task<AbhaVerificationSessionDto> InitiateAsync(
        long patientId, InitiateAbhaVerificationRequest request, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        await RequireM1ActiveAsync(ct);

        var challenge = await gateway.RequestOtpAsync(request.Method, request.Identifier.Trim(), ct);

        var session = new AbdmVerificationSession
        {
            PatientId = patientId,
            AbdmTxnId = challenge.TxnId,
            Method = request.Method,
            MaskedTarget = challenge.MaskedTarget,
            ExpiresAt = clock.GetUtcNow().Add(OtpSessionLifetime),
        };
        db.AbdmVerificationSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return new AbhaVerificationSessionDto(session.Id, session.MaskedTarget, session.ExpiresAt);
    }

    public async Task<AbhaProfileDto> CompleteAsync(
        long patientId, CompleteAbhaVerificationRequest request, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        await RequireM1ActiveAsync(ct);

        var session = await db.AbdmVerificationSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(AbdmVerificationSession), request.SessionId);

        if (session.IsConsumed)
        {
            throw new DomainException("This verification has already been completed. Start again.");
        }
        if (session.ExpiresAt < clock.GetUtcNow())
        {
            throw new DomainException("The OTP has expired. Request a new one.");
        }

        var abdm = await gateway.VerifyOtpAsync(session.AbdmTxnId, request.Otp.Trim(), ct);

        return await uow.ExecuteInTransactionAsync(async token =>
        {
            session.IsConsumed = true;

            var status = session.Method == AbdmVerificationMethod.AadhaarOtp
                ? AbhaLinkStatus.Created
                : AbhaLinkStatus.Linked;

            var profile = await db.AbhaProfiles.FirstOrDefaultAsync(p => p.PatientId == patientId, token);
            if (profile is null)
            {
                profile = new AbhaProfile { PatientId = patientId, AbhaNumber = abdm.AbhaNumber };
                db.AbhaProfiles.Add(profile);
            }
            profile.AbhaNumber = abdm.AbhaNumber;
            profile.AbhaAddress = abdm.AbhaAddress;
            profile.Status = status;
            profile.VerificationMethod = session.Method;
            profile.KycVerified = abdm.KycVerified;
            profile.LinkedName = abdm.Name;
            profile.LinkedGender = abdm.Gender;
            profile.LinkedYearOfBirth = abdm.YearOfBirth;
            profile.VerifiedAt = clock.GetUtcNow();

            await SyncPatientIdentifierAsync(patientId, abdm.AbhaNumber, token);

            await activity.LogAsync(ActivityEvents.AbhaLinked,
                $"ABHA {abdm.AbhaNumber} {status.ToString().ToLowerInvariant()} for patient #{patientId}", ct: token);
            await db.SaveChangesAsync(token);

            return Map(profile);
        }, ct);
    }

    public async Task<AbhaProfileDto> ScanAndShareAsync(
        long patientId, ScanAndShareQrRequest request, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        await RequireM1ActiveAsync(ct);

        var abdm = await gateway.ParseQrPayloadAsync(request.QrPayload, ct);

        return await uow.ExecuteInTransactionAsync(async token =>
        {
            var profile = await db.AbhaProfiles.FirstOrDefaultAsync(p => p.PatientId == patientId, token);
            if (profile is null)
            {
                profile = new AbhaProfile { PatientId = patientId, AbhaNumber = abdm.AbhaNumber };
                db.AbhaProfiles.Add(profile);
            }
            profile.AbhaNumber = abdm.AbhaNumber;
            profile.AbhaAddress = abdm.AbhaAddress;
            profile.Status = AbhaLinkStatus.Linked;
            profile.VerificationMethod = AbdmVerificationMethod.ScanAndShare;
            profile.KycVerified = abdm.KycVerified;
            profile.LinkedName = abdm.Name;
            profile.LinkedGender = abdm.Gender;
            profile.LinkedYearOfBirth = abdm.YearOfBirth;
            profile.VerifiedAt = clock.GetUtcNow();

            await SyncPatientIdentifierAsync(patientId, abdm.AbhaNumber, token);

            await activity.LogAsync(ActivityEvents.AbhaLinked,
                $"ABHA {abdm.AbhaNumber} linked via QR Scan & Share for patient #{patientId}", ct: token);
            await db.SaveChangesAsync(token);

            return Map(profile);
        }, ct);
    }

    public async Task UnlinkAsync(long patientId, UnlinkAbhaRequest request, CancellationToken ct = default)
    {
        await EnsurePatientExistsAsync(patientId, ct);
        var profile = await db.AbhaProfiles.FirstOrDefaultAsync(p => p.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(AbhaProfile), patientId);

        await uow.ExecuteInTransactionAsync(async token =>
        {
            var number = profile.AbhaNumber;
            profile.IsDeleted = true;

            var identifier = await db.PatientIdentifiers.FirstOrDefaultAsync(
                i => i.PatientId == patientId && i.Type == PatientIdentifierType.Abha, token);
            if (identifier is not null)
            {
                identifier.IsVerified = false;
            }

            await activity.LogAsync(ActivityEvents.AbhaUnlinked,
                $"ABHA {number} unlinked from patient #{patientId}: {request.Reason}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    private async Task SyncPatientIdentifierAsync(long patientId, string abhaNumber, CancellationToken ct)
    {
        var identifier = await db.PatientIdentifiers.FirstOrDefaultAsync(
            i => i.PatientId == patientId && i.Type == PatientIdentifierType.Abha, ct);
        if (identifier is null)
        {
            db.PatientIdentifiers.Add(new PatientIdentifier
            {
                PatientId = patientId,
                Type = PatientIdentifierType.Abha,
                Value = abhaNumber,
                IsVerified = true,
            });
        }
        else
        {
            identifier.Value = abhaNumber;
            identifier.IsVerified = true;
        }
    }

    private async Task RequireM1ActiveAsync(CancellationToken ct)
    {
        var master = await settings.GetBoolAsync(SettingKeys.AbdmEnabled, true, ct);
        if (!master)
        {
            throw new DomainException("ABDM integration is currently disabled in system settings. Contact your administrator to enable it.");
        }
        var m1 = await settings.GetBoolAsync(SettingKeys.AbdmM1Enabled, true, ct);
        if (!m1)
        {
            throw new DomainException("ABDM Milestone 1 (ABHA Creation & Verification) is currently disabled in system settings.");
        }
    }

    private async Task EnsurePatientExistsAsync(long patientId, CancellationToken ct)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
    }

    private static AbhaProfileDto Map(AbhaProfile p) => new(
        p.PatientId, p.AbhaNumber, p.AbhaAddress, p.Status, p.VerificationMethod,
        p.KycVerified, p.LinkedName, p.LinkedGender, p.LinkedYearOfBirth, p.VerifiedAt);
}
