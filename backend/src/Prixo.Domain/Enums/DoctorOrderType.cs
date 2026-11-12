namespace Prixo.Domain.Enums;

/// <summary>
/// The unified order-sheet discriminator (plan §9). Medication and Diet get a structured
/// child row (MedicationOrder/DietOrder); Lab/Radiology/Nursing stay free-text on the
/// header — same loose-coupling posture as OPD's ClinicalOrder (no auto-generated
/// LabOrder/RadiologyOrder from here yet).
/// </summary>
public enum DoctorOrderType
{
    Medication = 1,
    Lab = 2,
    Radiology = 3,
    Diet = 4,
    Nursing = 5,
}
