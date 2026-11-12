namespace Prixo.Domain.Enums;

/// <summary>
/// Classifies a Department. Only Clinical departments take patients, so OP registration and IP
/// admission offer (and enforce) Clinical departments only; NonClinical covers support and
/// administrative units (Laboratory, Radiology, Housekeeping, Finance…).
/// </summary>
public enum DepartmentType
{
    Clinical = 1,
    NonClinical = 2,
}
