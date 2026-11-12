using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Insurance;

/// <summary>
/// Pre-authorization request against a patient's insurance policy — the first stage of the
/// plan's "Pre-auth → enhancement → claim → settlement → denial tracking" flow. Modeled
/// directly on Ipd.AdmissionRequest (the codebase's closest existing pure request→approve/
/// reject template): single pair of decision columns, strict single-step state machine.
/// </summary>
public class PreAuthRequest : BranchEntity
{
    public required string PreAuthNumber { get; set; }

    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long PolicyId { get; set; }
    public PatientInsurancePolicy? Policy { get; set; }

    public PreAuthTreatmentType TreatmentType { get; set; }
    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public decimal EstimatedAmount { get; set; }
    public required string Diagnosis { get; set; }

    public long RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public DateTimeOffset RequestedAt { get; set; }

    public PreAuthStatus Status { get; set; } = PreAuthStatus.Requested;
    public decimal? ApprovedAmount { get; set; }
    public long? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<PreAuthEnhancement> Enhancements { get; set; } = [];
}
