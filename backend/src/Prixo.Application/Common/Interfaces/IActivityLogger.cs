namespace Prixo.Application.Common.Interfaces;

public static class ActivityEvents
{
    public const string Login = "LOGIN";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string TokenReuseDetected = "TOKEN_REUSE_DETECTED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string RolePermissionsChanged = "ROLE_PERMISSIONS_CHANGED";
    public const string UserRolesChanged = "USER_ROLES_CHANGED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    public const string UserPermissionOverridesChanged = "USER_PERMISSION_OVERRIDES_CHANGED";
    public const string SettingChanged = "SETTING_CHANGED";
    public const string PatientRegistered = "PATIENT_REGISTERED";
    public const string PatientMerged = "PATIENT_MERGED";
    public const string PatientBlocked = "PATIENT_BLOCKED";
    public const string PatientUnblocked = "PATIENT_UNBLOCKED";
    public const string AbhaLinked = "PATIENT_ABHA_LINKED";
    public const string AbhaUnlinked = "PATIENT_ABHA_UNLINKED";
    public const string AdmissionRequestCreated = "IPD_ADMISSION_REQUEST_CREATED";
    public const string AdmissionRequestApproved = "IPD_ADMISSION_REQUEST_APPROVED";
    public const string AdmissionRequestRejected = "IPD_ADMISSION_REQUEST_REJECTED";
    public const string AppointmentCancelled = "APPOINTMENT_CANCELLED";
    public const string AppointmentsAutoMarkedNoShow = "APPOINTMENTS_AUTO_MARKED_NO_SHOW";
    public const string AppointmentTransferred = "APPOINTMENT_TRANSFERRED";
    public const string OpVisitCancelled = "OPD_VISIT_CANCELLED";
    public const string MhcVisitCancelled = "MHC_VISIT_CANCELLED";
    public const string BillCancelled = "BILL_CANCELLED";
    public const string DiscountRequested = "DISCOUNT_REQUESTED";
    public const string DiscountApproved = "DISCOUNT_APPROVED";
    public const string DiscountRejected = "DISCOUNT_REJECTED";
    public const string RefundRequested = "REFUND_REQUESTED";
    public const string RefundApproved = "REFUND_APPROVED";
    public const string RefundIssued = "REFUND_ISSUED";
    public const string RefundRejected = "REFUND_REJECTED";
    public const string AdvanceDrawn = "ADVANCE_DRAWN";
    public const string AdvanceAutoDrawn = "ADVANCE_AUTO_DRAWN";
    public const string AdvanceTransferred = "ADVANCE_TRANSFERRED";
    public const string BedChargesAutoRefreshed = "BILLING_BED_CHARGES_AUTO_REFRESHED";
    public const string BillNoteAdded = "BILL_NOTE_ADDED";
    public const string BillParked = "BILL_PARKED";
    public const string BillUnparked = "BILL_UNPARKED";
    public const string CreditNoteRequested = "CREDIT_NOTE_REQUESTED";
    public const string CreditNoteApproved = "CREDIT_NOTE_APPROVED";
    public const string CreditNoteRejected = "CREDIT_NOTE_REJECTED";
    public const string DayClosed = "DAY_CLOSED";
    public const string CashierShiftClosed = "CASHIER_SHIFT_CLOSED";
    public const string CashierShiftReceived = "CASHIER_SHIFT_RECEIVED";
    public const string LabOrderCancelled = "LAB_ORDER_CANCELLED";
    public const string LabSampleRejected = "LAB_SAMPLE_REJECTED";
    public const string LabResultApproved = "LAB_RESULT_APPROVED";
    public const string LabCriticalAlertRaised = "LAB_CRITICAL_ALERT_RAISED";
    public const string LabCriticalAlertAcknowledged = "LAB_CRITICAL_ALERT_ACKNOWLEDGED";
    public const string GrnPosted = "PHARMACY_GRN_POSTED";
    public const string PurchaseReturnPosted = "PHARMACY_PURCHASE_RETURN_POSTED";
    public const string PharmacySaleCancelled = "PHARMACY_SALE_CANCELLED";
    public const string PharmacySalePayTypeModified = "PHARMACY_SALE_PAYTYPE_MODIFIED";
    public const string StockAdjustmentRequested = "PHARMACY_STOCK_ADJUSTMENT_REQUESTED";
    public const string StockAdjustmentApproved = "PHARMACY_STOCK_ADJUSTMENT_APPROVED";
    public const string StockAdjustmentRejected = "PHARMACY_STOCK_ADJUSTMENT_REJECTED";
    public const string StockTransferDispatched = "PHARMACY_STOCK_TRANSFER_DISPATCHED";
    public const string StockTransferReceived = "PHARMACY_STOCK_TRANSFER_RECEIVED";
    public const string StockAuditCompleted = "PHARMACY_STOCK_AUDIT_COMPLETED";
    public const string IndentRequested = "PHARMACY_INDENT_REQUESTED";
    public const string IndentApproved = "PHARMACY_INDENT_APPROVED";
    public const string IndentRejected = "PHARMACY_INDENT_REJECTED";
    public const string AdmissionCreated = "IPD_ADMISSION_CREATED";
    public const string AdmissionCancelled = "IPD_ADMISSION_CANCELLED";
    public const string BedTransferred = "IPD_BED_TRANSFERRED";
    public const string ConsultantTransferred = "IPD_CONSULTANT_TRANSFERRED";
    public const string DischargeRequested = "IPD_DISCHARGE_REQUESTED";
    public const string DischargeSummaryApproved = "IPD_DISCHARGE_SUMMARY_APPROVED";
    public const string PatientDischarged = "IPD_PATIENT_DISCHARGED";
    public const string RadOrderCancelled = "RAD_ORDER_CANCELLED";
    public const string RadReportApproved = "RAD_REPORT_APPROVED";
    public const string SurgeryBookingCreated = "OT_SURGERY_BOOKING_CREATED";
    public const string SurgeryBookingCancelled = "OT_SURGERY_BOOKING_CANCELLED";
    public const string SurgeryStarted = "OT_SURGERY_STARTED";
    public const string SurgeryCompleted = "OT_SURGERY_COMPLETED";
    public const string EmergencyVisitRegistered = "ER_VISIT_REGISTERED";
    public const string EmergencyVisitCancelled = "ER_VISIT_CANCELLED";
    public const string EmergencyDisposition = "ER_DISPOSITION_RECORDED";
    public const string MrdFileCreated = "MRD_FILE_CREATED";
    public const string MrdFileIssued = "MRD_FILE_ISSUED";
    public const string MrdFileReturned = "MRD_FILE_RETURNED";
    public const string MrdRecordDestroyed = "MRD_RECORD_DESTROYED";
    public const string FinanceVoucherPosted = "FINANCE_VOUCHER_POSTED";
    public const string FinanceVoucherCancelled = "FINANCE_VOUCHER_CANCELLED";
    public const string FinancePayableSettled = "FINANCE_PAYABLE_SETTLED";
    public const string FinanceReceivableCollected = "FINANCE_RECEIVABLE_COLLECTED";
    public const string BiomedWorkOrderCompleted = "BIOMED_WORK_ORDER_COMPLETED";
    public const string BiomedComplaintResolved = "BIOMED_COMPLAINT_RESOLVED";
    public const string PortalRegistered = "PORTAL_REGISTERED";
    public const string PortalLogin = "PORTAL_LOGIN";
    public const string PortalLoginFailed = "PORTAL_LOGIN_FAILED";
    public const string PortalLogout = "PORTAL_LOGOUT";
    public const string PortalPasswordChanged = "PORTAL_PASSWORD_CHANGED";
    public const string PortalTokenReuseDetected = "PORTAL_TOKEN_REUSE_DETECTED";
    public const string PortalFeedbackSubmitted = "PORTAL_FEEDBACK_SUBMITTED";
    public const string PortalOnlinePaymentRequested = "PORTAL_ONLINE_PAYMENT_REQUESTED";
    public const string OnlinePaymentRequestCompleted = "ONLINE_PAYMENT_REQUEST_COMPLETED";
    public const string OnlinePaymentRequestCancelled = "ONLINE_PAYMENT_REQUEST_CANCELLED";
    public const string LoyaltyCardDuplicateIssued = "LOYALTY_CARD_DUPLICATE_ISSUED";
    public const string LoyaltyCardDeactivated = "LOYALTY_CARD_DEACTIVATED";
    public const string PreAuthRequested = "INSURANCE_PREAUTH_REQUESTED";
    public const string PreAuthApproved = "INSURANCE_PREAUTH_APPROVED";
    public const string PreAuthRejected = "INSURANCE_PREAUTH_REJECTED";
    public const string PreAuthEnhancementRequested = "INSURANCE_PREAUTH_ENHANCEMENT_REQUESTED";
    public const string PreAuthEnhancementApproved = "INSURANCE_PREAUTH_ENHANCEMENT_APPROVED";
    public const string PreAuthEnhancementRejected = "INSURANCE_PREAUTH_ENHANCEMENT_REJECTED";
    public const string ClaimSubmitted = "INSURANCE_CLAIM_SUBMITTED";
    public const string ClaimStatusUpdated = "INSURANCE_CLAIM_STATUS_UPDATED";
    public const string ClaimSettled = "INSURANCE_CLAIM_SETTLED";
}

/// <summary>
/// Explicit business/security event log. Writes are saved with the caller's
/// SaveChanges (same transaction) unless saveImmediately is set.
/// </summary>
public interface IActivityLogger
{
    Task LogAsync(string eventType, string description,
        long? userId = null, string? username = null,
        bool saveImmediately = false, CancellationToken ct = default);
}
