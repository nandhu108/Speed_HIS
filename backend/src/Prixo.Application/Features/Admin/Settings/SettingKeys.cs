namespace Prixo.Application.Features.Admin.Settings;

/// <summary>Well-known system setting keys with their defaults (seeded at startup).</summary>
public static class SettingKeys
{
    public const string PasswordMinLength = "Password.MinLength";                    // 8
    public const string PasswordRequireUppercase = "Password.RequireUppercase";      // true
    public const string PasswordRequireLowercase = "Password.RequireLowercase";      // true
    public const string PasswordRequireDigit = "Password.RequireDigit";              // true
    public const string PasswordRequireSpecial = "Password.RequireSpecial";          // true
    public const string PasswordExpiryDays = "Password.ExpiryDays";                  // 0 = never
    public const string PasswordHistoryCount = "Password.HistoryCount";              // 5
    /// <summary>
    /// Whether a newly created or admin-reset account must change its password at first login.
    /// Defaults to true — a real hospital deployment wants it, and it is the control that stops a
    /// seeded/handed-out password from living forever. Switch it off at Admin › Settings for a
    /// dev/demo environment, where being bounced to the change-password screen on every fresh
    /// database is friction with nothing to protect.
    /// </summary>
    public const string PasswordForceChangeOnFirstLogin = "Password.ForceChangeOnFirstLogin"; // true

    public const string LockoutMaxFailedAttempts = "Lockout.MaxFailedAttempts";      // 5
    public const string LockoutMinutes = "Lockout.Minutes";                          // 15

    public const string SessionIdleTimeoutMinutes = "Session.IdleTimeoutMinutes";    // 30, 0 = disabled
    public const string SessionMaxConcurrent = "Session.MaxConcurrent";              // 0 = unlimited

    public const string BackupSchedule = "Backup.Schedule";                          // documented cron, informational
    public const string BackupRetentionDays = "Backup.RetentionDays";

    public const string NotificationSmsEnabled = "Notification.SmsEnabled";          // placeholder (Communication module)
    public const string NotificationEmailEnabled = "Notification.EmailEnabled";      // placeholder
    public const string NotificationWhatsAppEnabled = "Notification.WhatsAppEnabled";// placeholder

    public const string PatientUhidPrefix = "Patient.UhidPrefix";                    // "PRX"
    public const string PatientUhidDigits = "Patient.UhidDigits";                    // 6 -> PRX-2026-000123

    /// <summary>PACS viewer URL template (placeholder — Phase 12 PACS/DICOM integration not built). Empty by default.</summary>
    public const string RadiologyPacsViewerUrl = "Radiology.PacsViewerUrl";

    /// <summary>Custom footer line printed on the bill receipt PDF ("Control Panel - Billing"); empty falls back to the default thank-you line — see BillingService.GenerateReceiptAsync.</summary>
    public const string BillingReceiptFooterNote = "Billing.ReceiptFooterNote";

    /// <summary>Fallback consultation/follow-up fee used only when a doctor's own ConsultationFee/FollowUpFee is 0 (not configured) — see OpVisitService.RegisterAsync. 0 = no fallback.</summary>
    public const string BillingDefaultConsultationFee = "Billing.DefaultConsultationFee";
    /// <summary>
    /// OP diagnostics billing gate: "Paid" (default) — an OP lab sample cannot be collected and an OP
    /// scan cannot be scheduled until the test is on the patient's bill and that bill is settled (or
    /// is a credit bill); "Billed" — on the bill is enough; "None" — no gate. ER and IP orders are
    /// never gated (they are billed afterwards).
    /// </summary>
    public const string BillingDiagnosticsGate = "Billing.DiagnosticsGate";
    /// <summary>Service code charged per started hour of surgery when an OT booking is completed (empty = no automatic OT charge).</summary>
    public const string OtHourlyChargeServiceCode = "Ot.HourlyChargeServiceCode";
    public const string BillingDefaultFollowUpFee = "Billing.DefaultFollowUpFee";

    /// <summary>"Front Office control panel" — how many days ahead front-desk staff may book an appointment (0 = unlimited). Enforced only on the staff booking endpoint (AppointmentService.BookAsync), not reschedule/ER-token-mapping/Portal booking.</summary>
    public const string FrontOfficeMaxAdvanceBookingDays = "FrontOffice.MaxAdvanceBookingDays";

    /// <summary>Age (in completed years, strictly less-than) below which the OPIP Census Report buckets a patient as "Child" instead of Male/Female — see OpIpCensusReportService.</summary>
    public const string CensusChildAgeThresholdYears = "Census.ChildAgeThresholdYears";

    /// <summary>Ayushman Bharat Digital Mission (ABDM) master integration switch. When false, all ABDM endpoints report disabled.</summary>
    public const string AbdmEnabled = "Abdm.Enabled";

    /// <summary>ABDM Milestone 1 (M1): Enable/disable ABHA Creation, Verification & Linkage via Aadhaar OTP, Mobile OTP, and QR Scan.</summary>
    public const string AbdmM1Enabled = "Abdm.M1Enabled";

    /// <summary>ABDM Milestone 2 (M2): Enable/disable Health Information Provider (HIP) care-context linking and FHIR health data sharing.</summary>
    public const string AbdmM2Enabled = "Abdm.M2Enabled";

    /// <summary>ABDM Milestone 3 (M3): Enable/disable Health Information User (HIU) consent requests and viewing longitudinal external health records.</summary>
    public const string AbdmM3Enabled = "Abdm.M3Enabled";

    /// <summary>ABDM Gateway Base URL (e.g. sandbox https://dev.abdm.gov.in/gateway or production).</summary>
    public const string AbdmBaseUrl = "Abdm.BaseUrl";

    /// <summary>ABDM Client ID issued by National Health Authority (NHA).</summary>
    public const string AbdmClientId = "Abdm.ClientId";

    /// <summary>ABDM Client Secret issued by NHA.</summary>
    public const string AbdmClientSecret = "Abdm.ClientSecret";

    /// <summary>Health Facility Registry (HFR) Facility HIP ID (e.g. IN2910000123).</summary>
    public const string AbdmHipId = "Abdm.HipId";

    /// <summary>Health Information User (HIU) Facility ID registered in ABDM.</summary>
    public const string AbdmHiuId = "Abdm.HiuId";
}
