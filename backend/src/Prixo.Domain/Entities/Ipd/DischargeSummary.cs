using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>Structured, doctor-approved discharge summary (plan §9). One per admission; IsApproved gates the Discharge Summary PDF and final discharge.</summary>
public class DischargeSummary : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string FinalDiagnosis { get; set; }
    public string? CourseInHospital { get; set; }
    public string? MedicationsOnDischarge { get; set; }
    public string? FollowUpAdvice { get; set; }
    public string? ConditionAtDischarge { get; set; }

    public bool IsApproved { get; set; }
    public long? ApprovedByDoctorId { get; set; }
    public Doctor? ApprovedByDoctor { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}
