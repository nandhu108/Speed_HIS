using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Patients;

public record PatientContactDto(long Id, PatientContactType ContactType, string Name,
    string? Relationship, string? Phone, string? Email, string? Address, bool IsPrimary);
public record SavePatientContactRequest(PatientContactType ContactType, string Name,
    string? Relationship, string? Phone, string? Email, string? Address, bool IsPrimary = false);

public record PatientIdentifierDto(
    long Id, PatientIdentifierType Type, long? IdentityDocumentTypeId, string? IdentityDocumentTypeName,
    string Value, bool IsVerified);
public record SavePatientIdentifierRequest(PatientIdentifierType Type, long? IdentityDocumentTypeId, string Value);

public record PatientInsurancePolicyDto(
    long Id, long InsuranceCompanyId, string InsuranceCompanyName, string PolicyNumber,
    DateOnly? PolicyValidFrom, DateOnly? PolicyValidTo, bool IsPrimary);
public record SavePatientInsurancePolicyRequest(
    long InsuranceCompanyId, string PolicyNumber, DateOnly? PolicyValidFrom, DateOnly? PolicyValidTo,
    bool IsPrimary = false);

public record PatientListItemDto(
    long Id, string Uhid, string FullName, Gender Gender, string AgeDisplay, string Mobile,
    string? PatientCategoryName, string? PatientCategoryColorHex, string RegisteredBranchName,
    bool IsActive, bool IsBlocked, DateTimeOffset CreatedAt);

public record PatientDetailDto(
    long Id, string Uhid,
    long? SalutationId, string? SalutationName,
    string FirstName, string? MiddleName, string LastName, Gender Gender,
    DateOnly? DateOfBirth, int? EstimatedAge, string AgeDisplay, string? BloodGroup,
    long? MaritalStatusId, string? MaritalStatusName,
    string Mobile, string? AlternateMobile, string? WhatsAppNumber, string? Email,
    string? Address, string? PermanentAddress, bool IsPermanentSameAsPresent,
    long? StateId, string? StateName, long? CityId, string? CityName, long? AreaId, string? AreaName,
    string? Pincode, long? CountryId, string? CountryName, long? NationalityId, string? NationalityName,
    string? Occupation, decimal? MonthlyIncome,
    long? LanguageId, string? LanguageName,
    string? MotherName, string? NationalId,
    long? PatientCaseTypeId, string? PatientCaseTypeName,
    long? PatientHcTagId, string? PatientHcTagName,
    long? SpeciallyAbledCategoryId, string? SpeciallyAbledCategoryName,
    bool IsVip, bool SmsOptIn, bool InsuranceRequired, string? Remarks,
    long? PatientCategoryId, string? PatientCategoryName, string? PatientCategoryColorHex,
    long? ReferrerId, string? ReferrerName,
    long RegisteredBranchId, string RegisteredBranchName, string? PhotoPath, bool IsActive,
    bool IsBlocked, string? BlockedReason, DateTimeOffset? BlockedAt, string? BlockedByUserName,
    IReadOnlyList<PatientContactDto> Contacts, IReadOnlyList<PatientIdentifierDto> Identifiers,
    IReadOnlyList<PatientInsurancePolicyDto> InsurancePolicies);

public record BlockPatientRequest(string Reason);

public record RegisterPatientRequest(
    long? SalutationId, string FirstName, string? MiddleName, string LastName, Gender Gender,
    DateOnly? DateOfBirth, int? EstimatedAge, string? BloodGroup, long? MaritalStatusId,
    string Mobile, string? AlternateMobile, string? WhatsAppNumber, string? Email,
    string? Address, string? PermanentAddress, bool IsPermanentSameAsPresent,
    long? StateId, long? CityId, long? AreaId, string? Pincode, long? CountryId, long? NationalityId,
    string? Occupation, decimal? MonthlyIncome, long? LanguageId,
    string? MotherName, string? NationalId,
    long? PatientCaseTypeId, long? PatientHcTagId, long? SpeciallyAbledCategoryId,
    bool IsVip, bool SmsOptIn, bool InsuranceRequired, string? Remarks,
    long? PatientCategoryId, long? ReferrerId,
    IReadOnlyList<SavePatientContactRequest> Contacts, IReadOnlyList<SavePatientIdentifierRequest> Identifiers,
    IReadOnlyList<SavePatientInsurancePolicyRequest> InsurancePolicies);

public record UpdatePatientRequest(
    long? SalutationId, string FirstName, string? MiddleName, string LastName, Gender Gender,
    DateOnly? DateOfBirth, int? EstimatedAge, string? BloodGroup, long? MaritalStatusId,
    string Mobile, string? AlternateMobile, string? WhatsAppNumber, string? Email,
    string? Address, string? PermanentAddress, bool IsPermanentSameAsPresent,
    long? StateId, long? CityId, long? AreaId, string? Pincode, long? CountryId, long? NationalityId,
    string? Occupation, decimal? MonthlyIncome, long? LanguageId,
    string? MotherName, string? NationalId,
    long? PatientCaseTypeId, long? PatientHcTagId, long? SpeciallyAbledCategoryId,
    bool IsVip, bool SmsOptIn, bool InsuranceRequired, string? Remarks,
    long? PatientCategoryId, long? ReferrerId, bool IsActive,
    IReadOnlyList<SavePatientContactRequest> Contacts, IReadOnlyList<SavePatientIdentifierRequest> Identifiers,
    IReadOnlyList<SavePatientInsurancePolicyRequest> InsurancePolicies);

public record RegisterPatientResultDto(long Id, string Uhid);

public record CheckDuplicatesRequest(
    string FirstName, string LastName, DateOnly? DateOfBirth, string? Mobile, long? ExcludePatientId);

public record DuplicateCandidateDto(
    long Id, string Uhid, string FullName, Gender Gender, DateOnly? DateOfBirth,
    string Mobile, int MatchScore, IReadOnlyList<string> MatchReasons);

public record MergePatientRequest(long MergedPatientId, string Reason);

public record ConsentDto(long Id, long ConsentTemplateId, string TemplateTitle,
    string SignedByName, string? RelationshipToPatient, DateTimeOffset SignedAt, string? DocumentPath);
public record CaptureConsentRequest(long ConsentTemplateId, string SignedByName, string? RelationshipToPatient);

public record PatientDocumentDto(long Id, PatientDocumentType DocumentType, string FileName,
    string ContentType, long SizeBytes, string? Description, DateTimeOffset UploadedAt);
public record AddPatientDocumentRequest(PatientDocumentType DocumentType, string? Description);
