using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ot;

/// <summary>Pre-operative anaesthesia workup — one row per booking, recorded before the patient enters the theatre.</summary>
public class PreAnaesthesiaAssessment : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }

    public long AssessedByDoctorId { get; set; }
    public Doctor? AssessedByDoctor { get; set; }
    public DateTimeOffset AssessedAt { get; set; }

    /// <summary>Free text, e.g. "ASA II" — the emergency modifier ("II-E") doesn't fit a plain grade enum.</summary>
    public required string AsaGrade { get; set; }
    public string? AirwayAssessment { get; set; }
    public string? Comorbidities { get; set; }
    public string? Investigations { get; set; }
    public string? AnaesthesiaPlan { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Intra-operative anaesthesia record — one row per booking, recorded during the case.</summary>
public class IntraOpAnaesthesiaRecord : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }

    public AnaesthesiaType AnaesthesiaType { get; set; }
    public long AdministeredByDoctorId { get; set; }
    public Doctor? AdministeredByDoctor { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Notes { get; set; }
}
