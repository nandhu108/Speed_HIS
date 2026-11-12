using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>Billing category of a patient (General, Staff, Corporate…). Global; tariffs price per
/// category. Bespoke (not ISimpleMaster) since it carries ColorHex ("patient-type colors" — a
/// front-desk visual tag), same reasoning as DoctorPayoutRule/AppointmentType.</summary>
public class PatientCategory : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>"#RRGGBB", shown as a badge/dot next to the patient's category in front-desk lists.</summary>
    public string? ColorHex { get; set; }
}
