using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Single patient identity (UHID), global across all branches — the spine of the
/// system. RegisteredBranchId records where the record originated for reporting only;
/// it does not scope reads (patient history must be visible from every branch).
/// </summary>
public class Patient : BaseEntity
{
    public required string Uhid { get; set; }

    public long? SalutationId { get; set; }
    public Salutation? Salutation { get; set; }
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    /// <summary>Captured instead of DateOfBirth when the exact birth date is unknown.</summary>
    public int? EstimatedAge { get; set; }
    public string? BloodGroup { get; set; }
    public long? MaritalStatusId { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    public required string Mobile { get; set; }
    public string? AlternateMobile { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>Treated as the patient's present/current address.</summary>
    public string? Address { get; set; }
    public string? PermanentAddress { get; set; }
    /// <summary>When true, PermanentAddress is not separately captured — same as Address.</summary>
    public bool IsPermanentSameAsPresent { get; set; } = true;

    public long? StateId { get; set; }
    public State? State { get; set; }
    public long? CityId { get; set; }
    public City? City { get; set; }
    public long? AreaId { get; set; }
    public Area? Area { get; set; }
    public string? Pincode { get; set; }
    /// <summary>Country of residence — reuses the Nationality master (already just a Code/Name
    /// country list), distinct from NationalityId which captures citizenship.</summary>
    public long? CountryId { get; set; }
    public Nationality? Country { get; set; }
    public long? NationalityId { get; set; }
    public Nationality? Nationality { get; set; }

    public string? Occupation { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public long? LanguageId { get; set; }
    public Language? Language { get; set; }
    public string? MotherName { get; set; }
    public string? NationalId { get; set; }

    public long? PatientCaseTypeId { get; set; }
    public PatientCaseType? PatientCaseType { get; set; }
    public long? PatientHcTagId { get; set; }
    public PatientHcTag? PatientHcTag { get; set; }
    public long? SpeciallyAbledCategoryId { get; set; }
    public SpeciallyAbledCategory? SpeciallyAbledCategory { get; set; }

    public bool IsVip { get; set; }
    public bool SmsOptIn { get; set; } = true;
    /// <summary>Plain informational flag — no claims/TPA workflow triggers off this; checking it
    /// in the UI just reveals the policy sub-form that creates a PatientInsurancePolicy row.</summary>
    public bool InsuranceRequired { get; set; }
    public string? Remarks { get; set; }

    public long? PatientCategoryId { get; set; }
    public PatientCategory? PatientCategory { get; set; }
    public long? ReferrerId { get; set; }
    public Referrer? Referrer { get; set; }

    public long RegisteredBranchId { get; set; }
    public Branch? RegisteredBranch { get; set; }

    /// <summary>Stored path/URL only; served through a permission-gated download endpoint.</summary>
    public string? PhotoPath { get; set; }

    /// <summary>True once merged into another patient; the record is also soft-deleted.</summary>
    public bool IsMerged { get; set; }
    public long? MergedIntoPatientId { get; set; }

    /// <summary>Front-desk block (e.g. non-payment, fraud) — distinct from IsActive/soft-delete; a blocked patient's record still exists and is viewable, just flagged at registration/billing time.</summary>
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public DateTimeOffset? BlockedAt { get; set; }
    public long? BlockedByUserId { get; set; }
    public User? BlockedByUser { get; set; }

    public ICollection<PatientContact> Contacts { get; set; } = [];
    public ICollection<PatientIdentifier> Identifiers { get; set; } = [];
    public ICollection<PatientDocument> Documents { get; set; } = [];
    public ICollection<PatientConsent> Consents { get; set; } = [];
    public ICollection<PatientInsurancePolicy> InsurancePolicies { get; set; } = [];
}
