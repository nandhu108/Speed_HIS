using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// A patient's linked ABHA (Ayushman Bharat Health Account) — ABDM milestone M1. One active
/// profile per patient (partial-unique on PatientId where is_deleted = false). Distinct from
/// the generic <see cref="PatientIdentifier"/> row: this carries the full ABDM-returned
/// identity (number + address + demographic snapshot) and the verification provenance, while
/// the paired PatientIdentifier(Type = Abha) row is still upserted so existing consumers
/// (OP/IP reports) that read the ABHA number keep working unchanged.
///
/// Patients are global (not branch-scoped), so this extends <see cref="BaseEntity"/>.
/// </summary>
public class AbhaProfile : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>14-digit ABHA number, stored formatted (e.g. 91-1234-5678-9012).</summary>
    public required string AbhaNumber { get; set; }

    /// <summary>ABHA address / PHR address (e.g. name@sbx). Null until the patient sets one.</summary>
    public string? AbhaAddress { get; set; }

    public AbhaLinkStatus Status { get; set; }
    public AbdmVerificationMethod VerificationMethod { get; set; }

    /// <summary>True once ABDM has KYC-verified the holder (Aadhaar-backed link/creation).</summary>
    public bool KycVerified { get; set; }

    // Demographic snapshot ABDM returned at link time — kept for reconciliation against the
    // local patient record; never treated as the source of truth for the local demographics.
    public string? LinkedName { get; set; }
    public string? LinkedGender { get; set; }
    public int? LinkedYearOfBirth { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }
}
