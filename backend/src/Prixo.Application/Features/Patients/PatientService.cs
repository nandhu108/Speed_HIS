using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Utilities;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Patients;

public class PatientService(
    IAppDbContext db,
    ISequenceService sequences,
    SettingService settings,
    ICurrentUser currentUser,
    IActivityLogger activity,
    IUnitOfWork unitOfWork,
    IFileStorageService files,
    IPdfService pdf,
    TimeProvider timeProvider)
{
    private static readonly HashSet<string> AllowedPhotoTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private const long MaxDocumentBytes = 15 * 1024 * 1024;
    private const int DuplicateScoreThreshold = 50;

    public async Task<PagedResult<PatientListItemDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var patients = db.Patients.AsNoTracking();

        var words = string.IsNullOrWhiteSpace(query.Search) ? [] : query.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            // "First Last" (or "Last First", or with a middle name): every word must match some name part.
            foreach (var word in words)
            {
                var w = $"%{word}%";
                patients = patients.Where(p => EF.Functions.ILike(p.FirstName, w) || EF.Functions.ILike(p.LastName, w) ||
                    (p.MiddleName != null && EF.Functions.ILike(p.MiddleName, w)) || EF.Functions.ILike(p.Uhid, w));
            }
        }
        else if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var likeTerm = $"%{term}%";
            var dob = DateOnly.TryParse(term, out var parsedDob) ? parsedDob : (DateOnly?)null;
            patients = patients.Where(p =>
                EF.Functions.ILike(p.Uhid, likeTerm) ||
                EF.Functions.ILike(p.FirstName, likeTerm) ||
                EF.Functions.ILike(p.LastName, likeTerm) ||
                EF.Functions.ILike(p.Mobile, likeTerm) ||
                (p.AlternateMobile != null && EF.Functions.ILike(p.AlternateMobile, likeTerm)) ||
                (dob != null && p.DateOfBirth == dob));
        }

        var totalCount = await patients.CountAsync(ct);
        var now = timeProvider.GetUtcNow();

        var rows = await patients
            .OrderByDescending(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new
            {
                p.Id, p.Uhid, p.FirstName, p.MiddleName, p.LastName, p.Gender, p.DateOfBirth, p.EstimatedAge,
                p.Mobile, CategoryName = p.PatientCategory != null ? p.PatientCategory.Name : null,
                CategoryColorHex = p.PatientCategory != null ? p.PatientCategory.ColorHex : null,
                BranchName = p.RegisteredBranch!.Name, p.IsActive, p.IsBlocked, p.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rows.Select(p => new PatientListItemDto(
            p.Id, p.Uhid, FormatName(p.FirstName, p.MiddleName, p.LastName), p.Gender,
            FormatAge(p.DateOfBirth, p.EstimatedAge, now), p.Mobile, p.CategoryName, p.CategoryColorHex, p.BranchName,
            p.IsActive, p.IsBlocked, p.CreatedAt)).ToList();

        return PagedResult<PatientListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<PatientDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .Include(p => p.Contacts)
            .Include(p => p.Identifiers).ThenInclude(i => i.IdentityDocumentType)
            .Include(p => p.InsurancePolicies).ThenInclude(i => i.InsuranceCompany)
            .Include(p => p.PatientCategory)
            .Include(p => p.Referrer)
            .Include(p => p.RegisteredBranch)
            .Include(p => p.BlockedByUser)
            .Include(p => p.Salutation)
            .Include(p => p.MaritalStatus)
            .Include(p => p.State)
            .Include(p => p.City)
            .Include(p => p.Area)
            .Include(p => p.Country)
            .Include(p => p.Nationality)
            .Include(p => p.Language)
            .Include(p => p.PatientCaseType)
            .Include(p => p.PatientHcTag)
            .Include(p => p.SpeciallyAbledCategory)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Patient), id);

        return ToDetailDto(patient, timeProvider.GetUtcNow());
    }

    public async Task<RegisterPatientResultDto> RegisterAsync(RegisterPatientRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId
            ?? throw new DomainException("Select a branch before registering a patient.");
        await EnsureDemographicReferencesAsync(request, ct);

        if (!request.ConfirmDuplicate)
        {
            // Server-side safety net for the wizard's duplicate check: an exact match on name, date of
            // birth and mobile is refused unless the user has seen the warning and confirmed.
            var first = request.FirstName.Trim().ToLower();
            var last = request.LastName.Trim().ToLower();
            var mobile = request.Mobile.Trim();
            var duplicate = await db.Patients.AsNoTracking()
                .Where(p => p.Mobile == mobile && p.DateOfBirth == request.DateOfBirth &&
                    p.FirstName.ToLower() == first && p.LastName.ToLower() == last)
                .Select(p => p.Uhid).FirstOrDefaultAsync(ct);
            if (duplicate is not null)
            {
                throw new DomainException($"A patient with the same name, date of birth and mobile already exists ({duplicate}). Use the existing record, or confirm to register a separate person.");
            }
        }

        var uhid = await GenerateUhidAsync(ct);
        var patient = new Patient
        {
            Uhid = uhid,
            SalutationId = request.SalutationId,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            EstimatedAge = request.EstimatedAge,
            BloodGroup = request.BloodGroup,
            MaritalStatusId = request.MaritalStatusId,
            Mobile = request.Mobile,
            AlternateMobile = request.AlternateMobile,
            WhatsAppNumber = request.WhatsAppNumber,
            Email = request.Email,
            Address = request.Address,
            PermanentAddress = request.PermanentAddress,
            IsPermanentSameAsPresent = request.IsPermanentSameAsPresent,
            StateId = request.StateId,
            CityId = request.CityId,
            AreaId = request.AreaId,
            Pincode = request.Pincode,
            CountryId = request.CountryId,
            NationalityId = request.NationalityId,
            Occupation = request.Occupation,
            MonthlyIncome = request.MonthlyIncome,
            LanguageId = request.LanguageId,
            MotherName = request.MotherName,
            NationalId = request.NationalId,
            PatientCaseTypeId = request.PatientCaseTypeId,
            PatientHcTagId = request.PatientHcTagId,
            SpeciallyAbledCategoryId = request.SpeciallyAbledCategoryId,
            IsVip = request.IsVip,
            SmsOptIn = request.SmsOptIn,
            InsuranceRequired = request.InsuranceRequired,
            Remarks = request.Remarks,
            PatientCategoryId = request.PatientCategoryId,
            ReferrerId = request.ReferrerId,
            RegisteredBranchId = branchId,
            Contacts = request.Contacts.Select(ToContactEntity).ToList(),
            Identifiers = request.Identifiers.Select(ToIdentifierEntity).ToList(),
            InsurancePolicies = request.InsurancePolicies.Select(ToInsurancePolicyEntity).ToList(),
        };

        db.Patients.Add(patient);
        await activity.LogAsync(ActivityEvents.PatientRegistered,
            $"Patient '{FormatName(request.FirstName, request.MiddleName, request.LastName)}' registered with UHID {uhid}",
            ct: ct);
        await db.SaveChangesAsync(ct);

        return new RegisterPatientResultDto(patient.Id, patient.Uhid);
    }

    public async Task UpdateAsync(long id, UpdatePatientRequest request, CancellationToken ct = default)
    {
        var patient = await db.Patients
            .Include(p => p.Contacts)
            .Include(p => p.Identifiers)
            .Include(p => p.InsurancePolicies)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Patient), id);
        await EnsureDemographicReferencesAsync(request, ct);

        patient.SalutationId = request.SalutationId;
        patient.FirstName = request.FirstName;
        patient.MiddleName = request.MiddleName;
        patient.LastName = request.LastName;
        patient.Gender = request.Gender;
        patient.DateOfBirth = request.DateOfBirth;
        patient.EstimatedAge = request.EstimatedAge;
        patient.BloodGroup = request.BloodGroup;
        patient.MaritalStatusId = request.MaritalStatusId;
        patient.Mobile = request.Mobile;
        patient.AlternateMobile = request.AlternateMobile;
        patient.WhatsAppNumber = request.WhatsAppNumber;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.PermanentAddress = request.PermanentAddress;
        patient.IsPermanentSameAsPresent = request.IsPermanentSameAsPresent;
        patient.StateId = request.StateId;
        patient.CityId = request.CityId;
        patient.AreaId = request.AreaId;
        patient.Pincode = request.Pincode;
        patient.CountryId = request.CountryId;
        patient.NationalityId = request.NationalityId;
        patient.Occupation = request.Occupation;
        patient.MonthlyIncome = request.MonthlyIncome;
        patient.LanguageId = request.LanguageId;
        patient.MotherName = request.MotherName;
        patient.NationalId = request.NationalId;
        patient.PatientCaseTypeId = request.PatientCaseTypeId;
        patient.PatientHcTagId = request.PatientHcTagId;
        patient.SpeciallyAbledCategoryId = request.SpeciallyAbledCategoryId;
        patient.IsVip = request.IsVip;
        patient.SmsOptIn = request.SmsOptIn;
        patient.InsuranceRequired = request.InsuranceRequired;
        patient.Remarks = request.Remarks;
        patient.PatientCategoryId = request.PatientCategoryId;
        patient.ReferrerId = request.ReferrerId;
        patient.IsActive = request.IsActive;

        db.PatientContacts.RemoveRange(patient.Contacts);
        patient.Contacts = request.Contacts.Select(ToContactEntity).ToList();
        db.PatientIdentifiers.RemoveRange(patient.Identifiers);
        patient.Identifiers = request.Identifiers.Select(ToIdentifierEntity).ToList();
        db.PatientInsurancePolicies.RemoveRange(patient.InsurancePolicies);
        patient.InsurancePolicies = request.InsurancePolicies.Select(ToInsurancePolicyEntity).ToList();

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DuplicateCandidateDto>> CheckDuplicatesAsync(
        CheckDuplicatesRequest request, CancellationToken ct = default)
    {
        var mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        var candidates = db.Patients.AsNoTracking()
            .Where(p => request.ExcludePatientId == null || p.Id != request.ExcludePatientId);

        candidates = (mobile, request.DateOfBirth) switch
        {
            (not null, { } dob) => candidates.Where(p =>
                p.Mobile == mobile || p.AlternateMobile == mobile || p.DateOfBirth == dob),
            (not null, null) => candidates.Where(p => p.Mobile == mobile || p.AlternateMobile == mobile),
            (null, { } dob) => candidates.Where(p => p.DateOfBirth == dob),
            _ => candidates.Take(0), // nothing to prefilter on — avoid a full table scan
        };

        var prefiltered = await candidates.Take(200).ToListAsync(ct);
        var fullName = $"{request.FirstName} {request.LastName}";

        var scored = prefiltered
            .Select(p => ScoreCandidate(p, fullName, request.DateOfBirth, mobile))
            .Where(x => x.Score >= DuplicateScoreThreshold)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        return scored.Select(x => new DuplicateCandidateDto(
            x.Patient.Id, x.Patient.Uhid, FormatName(x.Patient.FirstName, x.Patient.MiddleName, x.Patient.LastName),
            x.Patient.Gender, x.Patient.DateOfBirth, x.Patient.Mobile, x.Score, x.Reasons)).ToList();
    }

    public async Task<IReadOnlyList<DuplicateCandidateDto>> GetDuplicatesForPatientAsync(
        long id, CancellationToken ct = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Patient), id);
        return await CheckDuplicatesAsync(
            new CheckDuplicatesRequest(patient.FirstName, patient.LastName, patient.DateOfBirth, patient.Mobile, patient.Id),
            ct);
    }

    /// <summary>Re-points every FK from the merged patient to the survivor in one transaction.</summary>
    public async Task MergeAsync(long survivorId, MergePatientRequest request, CancellationToken ct = default)
    {
        if (survivorId == request.MergedPatientId)
        {
            throw new DomainException("A patient cannot be merged into itself.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var survivor = await db.Patients.FirstOrDefaultAsync(p => p.Id == survivorId, token)
                ?? throw new NotFoundException(nameof(Patient), survivorId);
            var merged = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.MergedPatientId, token)
                ?? throw new NotFoundException(nameof(Patient), request.MergedPatientId);

            if (survivor.IsMerged || merged.IsMerged)
            {
                throw new DomainException("One of the selected patients has already been merged.");
            }

            var contacts = await db.PatientContacts.Where(c => c.PatientId == merged.Id).ToListAsync(token);
            foreach (var contact in contacts)
            {
                contact.PatientId = survivor.Id;
            }

            var identifiers = await db.PatientIdentifiers.Where(i => i.PatientId == merged.Id).ToListAsync(token);
            foreach (var identifier in identifiers)
            {
                identifier.PatientId = survivor.Id;
            }

            var documents = await db.PatientDocuments.Where(d => d.PatientId == merged.Id).ToListAsync(token);
            foreach (var document in documents)
            {
                document.PatientId = survivor.Id;
            }

            var consents = await db.PatientConsents.Where(c => c.PatientId == merged.Id).ToListAsync(token);
            foreach (var consent in consents)
            {
                consent.PatientId = survivor.Id;
            }

            merged.IsMerged = true;
            merged.MergedIntoPatientId = survivor.Id;
            db.Patients.Remove(merged); // converted to soft delete by the interceptor

            db.PatientMerges.Add(new PatientMerge
            {
                SurvivorPatientId = survivor.Id,
                MergedPatientId = merged.Id,
                Reason = request.Reason,
            });

            await activity.LogAsync(ActivityEvents.PatientMerged,
                $"Patient '{merged.Uhid}' merged into '{survivor.Uhid}': {request.Reason}", ct: token);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task BlockAsync(long id, BlockPatientRequest request, CancellationToken ct = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Patient), id);
        if (patient.IsBlocked)
        {
            throw new DomainException("Patient is already blocked.");
        }

        patient.IsBlocked = true;
        patient.BlockedReason = request.Reason;
        patient.BlockedAt = timeProvider.GetUtcNow();
        patient.BlockedByUserId = currentUser.UserId;

        await activity.LogAsync(ActivityEvents.PatientBlocked,
            $"Patient '{patient.Uhid}' blocked: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UnblockAsync(long id, CancellationToken ct = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Patient), id);
        if (!patient.IsBlocked)
        {
            throw new DomainException("Patient is not blocked.");
        }

        patient.IsBlocked = false;
        patient.BlockedReason = null;
        patient.BlockedAt = null;
        patient.BlockedByUserId = null;

        await activity.LogAsync(ActivityEvents.PatientUnblocked,
            $"Patient '{patient.Uhid}' unblocked", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SavePhotoAsync(
        long patientId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken ct = default)
    {
        if (!AllowedPhotoTypes.Contains(contentType))
        {
            throw new DomainException("Only JPEG, PNG or WEBP photos are allowed.");
        }
        if (sizeBytes > MaxPhotoBytes)
        {
            throw new DomainException("Photo must be under 5 MB.");
        }

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        var stored = await files.SaveAsync(content, $"patients/{patient.Uhid}", fileName, ct);
        patient.PhotoPath = stored.RelativePath;
        await db.SaveChangesAsync(ct);
    }

    public async Task<(Stream Content, string ContentType)> OpenPhotoAsync(long patientId, CancellationToken ct = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);
        if (patient.PhotoPath is null)
        {
            throw new NotFoundException("Patient photo", patientId);
        }

        var stream = await files.OpenReadAsync(patient.PhotoPath, ct);
        var contentType = Path.GetExtension(patient.PhotoPath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
        return (stream, contentType);
    }

    public async Task<long> AddDocumentAsync(
        long patientId, PatientDocumentType documentType, Stream content, string fileName, string contentType,
        long sizeBytes, string? description, CancellationToken ct = default)
    {
        if (sizeBytes > MaxDocumentBytes)
        {
            throw new DomainException("File must be under 15 MB.");
        }

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        var stored = await files.SaveAsync(content, $"patients/{patient.Uhid}", fileName, ct);
        var document = new PatientDocument
        {
            PatientId = patientId,
            DocumentType = documentType,
            FileName = fileName,
            FilePath = stored.RelativePath,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            Description = description,
        };
        db.PatientDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document.Id;
    }

    public async Task<IReadOnlyList<PatientDocumentDto>> GetDocumentsAsync(long patientId, CancellationToken ct = default) =>
        await db.PatientDocuments.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.Id)
            .Select(d => new PatientDocumentDto(
                d.Id, d.DocumentType, d.FileName, d.ContentType, d.SizeBytes, d.Description, d.CreatedAt))
            .ToListAsync(ct);

    public async Task<(Stream Content, string ContentType, string FileName)> OpenDocumentAsync(
        long patientId, long documentId, CancellationToken ct = default)
    {
        var document = await db.PatientDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(PatientDocument), documentId);
        var stream = await files.OpenReadAsync(document.FilePath, ct);
        return (stream, document.ContentType, document.FileName);
    }

    public async Task<long> CaptureConsentAsync(
        long patientId, CaptureConsentRequest request, Stream? scan, string? scanFileName,
        CancellationToken ct = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);
        if (!await db.ConsentTemplates.AnyAsync(c => c.Id == request.ConsentTemplateId, ct))
        {
            throw new DomainException("Consent template does not exist.");
        }

        string? scanPath = null;
        if (scan is not null && scanFileName is not null)
        {
            var stored = await files.SaveAsync(scan, $"patients/{patient.Uhid}/consents", scanFileName, ct);
            scanPath = stored.RelativePath;
        }

        var consent = new PatientConsent
        {
            PatientId = patientId,
            ConsentTemplateId = request.ConsentTemplateId,
            SignedByName = request.SignedByName,
            RelationshipToPatient = request.RelationshipToPatient,
            SignedAt = timeProvider.GetUtcNow(),
            DocumentPath = scanPath,
        };
        db.PatientConsents.Add(consent);
        await db.SaveChangesAsync(ct);
        return consent.Id;
    }

    public async Task<IReadOnlyList<ConsentDto>> GetConsentsAsync(long patientId, CancellationToken ct = default) =>
        await db.PatientConsents.AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.Id)
            .Select(c => new ConsentDto(
                c.Id, c.ConsentTemplateId, c.ConsentTemplate!.Title, c.SignedByName, c.RelationshipToPatient,
                c.SignedAt, c.DocumentPath))
            .ToListAsync(ct);

    public async Task<byte[]> GenerateRegistrationCardAsync(long patientId, CancellationToken ct = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .Include(p => p.RegisteredBranch)
            .FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        byte[]? photoBytes = null;
        if (patient.PhotoPath is not null)
        {
            try
            {
                await using var photoStream = await files.OpenReadAsync(patient.PhotoPath, ct);
                using var buffer = new MemoryStream();
                await photoStream.CopyToAsync(buffer, ct);
                photoBytes = buffer.ToArray();
            }
            catch (FileNotFoundException)
            {
                // Photo missing on disk — print the card without it.
            }
        }

        var now = timeProvider.GetUtcNow();
        var data = new RegistrationCardData(
            patient.Uhid,
            FormatName(patient.FirstName, patient.MiddleName, patient.LastName),
            patient.Gender.ToString(),
            FormatAge(patient.DateOfBirth, patient.EstimatedAge, now),
            patient.Mobile,
            patient.Address,
            patient.RegisteredBranch?.Name ?? string.Empty,
            patient.RegisteredBranch?.Address ?? string.Empty,
            patient.CreatedAt,
            photoBytes);

        return pdf.GenerateRegistrationCard(data);
    }

    private async Task<string> GenerateUhidAsync(CancellationToken ct)
    {
        var year = timeProvider.GetUtcNow().Year;
        var nextValue = await sequences.NextValueAsync(SequenceKeys.Uhid, scopeId: 0, year, ct);
        var prefix = await settings.GetStringAsync(SettingKeys.PatientUhidPrefix, "PRX", ct);
        var digits = await settings.GetIntAsync(SettingKeys.PatientUhidDigits, 6, ct);
        return $"{prefix}-{year}-{nextValue.ToString().PadLeft(digits, '0')}";
    }

    private async Task EnsureReferencesAsync(long? patientCategoryId, long? referrerId, CancellationToken ct)
    {
        if (patientCategoryId is not null && !await db.PatientCategories.AnyAsync(c => c.Id == patientCategoryId, ct))
        {
            throw new DomainException("Patient category does not exist.");
        }
        if (referrerId is not null && !await db.Referrers.AnyAsync(r => r.Id == referrerId, ct))
        {
            throw new DomainException("Referrer does not exist.");
        }
    }

    private async Task EnsureDemographicReferencesAsync(
        RegisterPatientRequest request, CancellationToken ct)
    {
        await EnsureReferencesAsync(request.PatientCategoryId, request.ReferrerId, ct);
        await EnsureExistsAsync(db.Salutations, request.SalutationId, "Salutation", ct);
        await EnsureExistsAsync(db.MaritalStatuses, request.MaritalStatusId, "Marital status", ct);
        await EnsureExistsAsync(db.States, request.StateId, "State", ct);
        await EnsureExistsAsync(db.Cities, request.CityId, "City", ct);
        await EnsureExistsAsync(db.Areas, request.AreaId, "Area", ct);
        await EnsureExistsAsync(db.Nationalities, request.CountryId, "Country", ct);
        await EnsureExistsAsync(db.Nationalities, request.NationalityId, "Nationality", ct);
        await EnsureExistsAsync(db.Languages, request.LanguageId, "Language", ct);
        await EnsureExistsAsync(db.PatientCaseTypes, request.PatientCaseTypeId, "Patient case type", ct);
        await EnsureExistsAsync(db.PatientHcTags, request.PatientHcTagId, "Patient tag", ct);
        await EnsureExistsAsync(db.SpeciallyAbledCategories, request.SpeciallyAbledCategoryId, "Specially-abled category", ct);
        foreach (var policy in request.InsurancePolicies)
        {
            await EnsureExistsAsync(db.InsuranceCompanies, policy.InsuranceCompanyId, "Insurance company", ct);
        }
    }

    private async Task EnsureDemographicReferencesAsync(
        UpdatePatientRequest request, CancellationToken ct)
    {
        await EnsureReferencesAsync(request.PatientCategoryId, request.ReferrerId, ct);
        await EnsureExistsAsync(db.Salutations, request.SalutationId, "Salutation", ct);
        await EnsureExistsAsync(db.MaritalStatuses, request.MaritalStatusId, "Marital status", ct);
        await EnsureExistsAsync(db.States, request.StateId, "State", ct);
        await EnsureExistsAsync(db.Cities, request.CityId, "City", ct);
        await EnsureExistsAsync(db.Areas, request.AreaId, "Area", ct);
        await EnsureExistsAsync(db.Nationalities, request.CountryId, "Country", ct);
        await EnsureExistsAsync(db.Nationalities, request.NationalityId, "Nationality", ct);
        await EnsureExistsAsync(db.Languages, request.LanguageId, "Language", ct);
        await EnsureExistsAsync(db.PatientCaseTypes, request.PatientCaseTypeId, "Patient case type", ct);
        await EnsureExistsAsync(db.PatientHcTags, request.PatientHcTagId, "Patient tag", ct);
        await EnsureExistsAsync(db.SpeciallyAbledCategories, request.SpeciallyAbledCategoryId, "Specially-abled category", ct);
        foreach (var policy in request.InsurancePolicies)
        {
            await EnsureExistsAsync(db.InsuranceCompanies, policy.InsuranceCompanyId, "Insurance company", ct);
        }
    }

    private static async Task EnsureExistsAsync<TEntity>(
        IQueryable<TEntity> set, long? id, string label, CancellationToken ct) where TEntity : Domain.Common.BaseEntity
    {
        if (id is not null && !await set.AnyAsync(e => e.Id == id, ct))
        {
            throw new DomainException($"{label} does not exist.");
        }
    }

    private static (Patient Patient, int Score, List<string> Reasons) ScoreCandidate(
        Patient candidate, string fullName, DateOnly? dateOfBirth, string? mobile)
    {
        var score = 0;
        var reasons = new List<string>();

        if (mobile is not null && (candidate.Mobile == mobile || candidate.AlternateMobile == mobile))
        {
            score += 60;
            reasons.Add("Mobile number matches");
        }
        if (dateOfBirth is not null && candidate.DateOfBirth == dateOfBirth)
        {
            score += 35;
            reasons.Add("Date of birth matches");
        }

        var similarity = TextSimilarity.NameSimilarity(fullName, $"{candidate.FirstName} {candidate.LastName}");
        if (similarity >= 0.85)
        {
            score += 30;
            reasons.Add("Name is a strong match");
        }
        else if (similarity >= 0.7)
        {
            score += 15;
            reasons.Add("Name is similar");
        }
        else if (similarity >= 0.5)
        {
            score += 5;
        }

        return (candidate, score, reasons);
    }

    private static PatientContact ToContactEntity(SavePatientContactRequest request) => new()
    {
        ContactType = request.ContactType,
        Name = request.Name,
        Relationship = request.Relationship,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        IsPrimary = request.IsPrimary,
    };

    private static PatientIdentifier ToIdentifierEntity(SavePatientIdentifierRequest request) => new()
    {
        Type = request.Type,
        IdentityDocumentTypeId = request.IdentityDocumentTypeId,
        Value = request.Value,
    };

    private static PatientInsurancePolicy ToInsurancePolicyEntity(SavePatientInsurancePolicyRequest request) => new()
    {
        InsuranceCompanyId = request.InsuranceCompanyId,
        PolicyNumber = request.PolicyNumber,
        PolicyValidFrom = request.PolicyValidFrom,
        PolicyValidTo = request.PolicyValidTo,
        IsPrimary = request.IsPrimary,
    };

    private static PatientDetailDto ToDetailDto(Patient patient, DateTimeOffset now) => new(
        patient.Id, patient.Uhid,
        patient.SalutationId, patient.Salutation?.Name,
        patient.FirstName, patient.MiddleName, patient.LastName, patient.Gender,
        patient.DateOfBirth, patient.EstimatedAge, FormatAge(patient.DateOfBirth, patient.EstimatedAge, now),
        patient.BloodGroup, patient.MaritalStatusId, patient.MaritalStatus?.Name,
        patient.Mobile, patient.AlternateMobile, patient.WhatsAppNumber, patient.Email,
        patient.Address, patient.PermanentAddress, patient.IsPermanentSameAsPresent,
        patient.StateId, patient.State?.Name, patient.CityId, patient.City?.Name, patient.AreaId, patient.Area?.Name,
        patient.Pincode, patient.CountryId, patient.Country?.Name, patient.NationalityId, patient.Nationality?.Name,
        patient.Occupation, patient.MonthlyIncome,
        patient.LanguageId, patient.Language?.Name,
        patient.MotherName, patient.NationalId,
        patient.PatientCaseTypeId, patient.PatientCaseType?.Name,
        patient.PatientHcTagId, patient.PatientHcTag?.Name,
        patient.SpeciallyAbledCategoryId, patient.SpeciallyAbledCategory?.Name,
        patient.IsVip, patient.SmsOptIn, patient.InsuranceRequired, patient.Remarks,
        patient.PatientCategoryId, patient.PatientCategory?.Name, patient.PatientCategory?.ColorHex,
        patient.ReferrerId, patient.Referrer?.Name,
        patient.RegisteredBranchId, patient.RegisteredBranch?.Name ?? string.Empty, patient.PhotoPath, patient.IsActive,
        patient.IsBlocked, patient.BlockedReason, patient.BlockedAt, patient.BlockedByUser?.FullName,
        patient.Contacts.Select(c => new PatientContactDto(
            c.Id, c.ContactType, c.Name, c.Relationship, c.Phone, c.Email, c.Address, c.IsPrimary)).ToList(),
        patient.Identifiers.Select(i => new PatientIdentifierDto(
            i.Id, i.Type, i.IdentityDocumentTypeId, i.IdentityDocumentType?.Name, i.Value, i.IsVerified)).ToList(),
        patient.InsurancePolicies.Select(p => new PatientInsurancePolicyDto(
            p.Id, p.InsuranceCompanyId, p.InsuranceCompany?.Name ?? string.Empty, p.PolicyNumber,
            p.PolicyValidFrom, p.PolicyValidTo, p.IsPrimary)).ToList());

    private static string FormatName(string firstName, string? middleName, string lastName) =>
        string.Join(' ', new[] { firstName, middleName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

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
