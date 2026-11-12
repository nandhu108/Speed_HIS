using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// Pre-admission request — a front-desk "please admit this patient" ask that a request→
/// approve/reject workflow gates before the existing direct-admit flow (AdmissionService.
/// AdmitAsync) runs, mirroring Stores' indent→approve→issue pattern. Approving does not
/// itself create the Admission (that still needs a bed pick) — it just clears the request
/// to proceed; ApprovedAdmissionId is stamped once AdmitAsync is actually called against it.
/// </summary>
public class AdmissionRequest : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public long? RequestedConsultantId { get; set; }
    public Doctor? RequestedConsultant { get; set; }
    public long? WardId { get; set; }
    public Ward? Ward { get; set; }
    public DateOnly ExpectedAdmissionDate { get; set; }
    public string? Reason { get; set; }

    public AdmissionRequestStatus Status { get; set; } = AdmissionRequestStatus.Pending;
    public string? RejectionReason { get; set; }

    public long RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public long? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public long? ApprovedAdmissionId { get; set; }
    public Admission? ApprovedAdmission { get; set; }
}
