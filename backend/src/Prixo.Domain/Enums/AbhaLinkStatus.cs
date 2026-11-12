namespace Prixo.Domain.Enums;

/// <summary>Lifecycle of a patient's ABHA (Ayushman Bharat Health Account) link, per ABDM M1.</summary>
public enum AbhaLinkStatus
{
    /// <summary>OTP challenge issued, awaiting verification — no ABHA bound yet.</summary>
    Pending = 1,
    /// <summary>An existing ABHA was verified and linked to this patient.</summary>
    Linked = 2,
    /// <summary>A new ABHA was created for this patient through ABDM.</summary>
    Created = 3,
    /// <summary>Verification failed or the link was revoked.</summary>
    Failed = 4,
}
