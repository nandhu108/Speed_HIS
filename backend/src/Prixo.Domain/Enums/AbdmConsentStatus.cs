namespace Prixo.Domain.Enums;

/// <summary>Status lifecycle of an ABDM Consent Request (Milestone M3 - HIU).</summary>
public enum AbdmConsentStatus
{
    Requested = 1,
    Granted = 2,
    Denied = 3,
    Revoked = 4,
    Expired = 5,
}
