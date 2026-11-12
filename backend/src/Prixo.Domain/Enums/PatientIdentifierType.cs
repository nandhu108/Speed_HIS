namespace Prixo.Domain.Enums;

/// <summary>Aadhaar/ABHA are placeholder fields — capture only, verification is a held integration.</summary>
public enum PatientIdentifierType
{
    Aadhaar = 1,
    Abha = 2,
    /// <summary>Generalized identity document — see PatientIdentifier.IdentityDocumentTypeId
    /// (PAN/Passport/Voter ID/Driving License/Ration Card/...).</summary>
    Other = 3,
}
