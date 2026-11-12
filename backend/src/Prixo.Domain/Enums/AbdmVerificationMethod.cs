namespace Prixo.Domain.Enums;

/// <summary>How a patient proves control of an ABHA during ABDM M1 verification/creation.</summary>
public enum AbdmVerificationMethod
{
    /// <summary>OTP sent to the Aadhaar-linked mobile (Aadhaar-based, used for ABHA creation and KYC).</summary>
    AadhaarOtp = 1,
    /// <summary>OTP sent to the ABHA-registered mobile number (mobile-based linkage).</summary>
    MobileOtp = 2,
    /// <summary>OTP challenge against an existing ABHA number / ABHA address.</summary>
    AbhaOtp = 3,
    /// <summary>QR code scan-and-share from the patient's ABHA PHR app.</summary>
    ScanAndShare = 4,
}
