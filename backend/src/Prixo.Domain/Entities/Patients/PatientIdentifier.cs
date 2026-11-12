using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Aadhaar/ABHA placeholder — capture only. IsVerified stays false until the
/// UIDAI/ABDM integration (held) lands; the frontend verify button is disabled.
/// Type == Other generalizes to any master-driven identity document (PAN/Passport/...)
/// via IdentityDocumentTypeId, since CLAUDE.md requires every dropdown to be a master.
/// </summary>
public class PatientIdentifier : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public PatientIdentifierType Type { get; set; }
    /// <summary>Set only when Type == Other.</summary>
    public long? IdentityDocumentTypeId { get; set; }
    public IdentityDocumentType? IdentityDocumentType { get; set; }
    public required string Value { get; set; }
    public bool IsVerified { get; set; }
}
