namespace Prixo.Domain.Enums;

/// <summary>Polymorphic order header type — each type lands in its own module's worklist once that module exists.</summary>
public enum ClinicalOrderType
{
    Lab = 1,
    Radiology = 2,
    Procedure = 3,
}
