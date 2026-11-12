namespace Prixo.Application.Common.Interfaces;

/// <summary>Well-known sequence keys. Formats are owned by the calling service (e.g. PRX-2026-000123).</summary>
public static class SequenceKeys
{
    public const string Uhid = "UHID";
    /// <summary>Appointment token number, scoped per doctor and reset daily (see AppointmentService).</summary>
    public const string AppointmentToken = "APPT_TOKEN";
    /// <summary>OPD visit number, scoped per branch and year — e.g. OPD-2026-000123.</summary>
    public const string OpVisitNumber = "OPD_VISIT";
    /// <summary>Walk-in (no prior appointment) OPD token, scoped per doctor and reset daily.</summary>
    public const string OpVisitToken = "OPD_TOKEN";
    /// <summary>MHC visit number, scoped per branch and year — e.g. MHC-2026-000123.</summary>
    public const string MhcVisitNumber = "MHC_VISIT";
    /// <summary>MHC token, scoped per MHC doctor and reset daily (own key so it never shares a
    /// counter with an OpVisitToken scope even if a MhcDoctorId numerically matches a DoctorId).</summary>
    public const string MhcVisitToken = "MHC_TOKEN";
    /// <summary>Bill number, scoped per branch and year — e.g. BILL-2026-000123.</summary>
    public const string BillNumber = "BILL_NUMBER";
    /// <summary>Payment receipt number, scoped per branch and year.</summary>
    public const string ReceiptNumber = "RECEIPT_NUMBER";
    /// <summary>Bill credit note number, scoped per branch and year — e.g. CN-2026-000123.</summary>
    public const string CreditNoteNumber = "CREDIT_NOTE_NUMBER";
    /// <summary>Bill estimate number, scoped per branch and year — e.g. EST-2026-000123.</summary>
    public const string BillEstimateNumber = "BILL_ESTIMATE_NUMBER";
    /// <summary>Lab order number, scoped per branch and year — e.g. LAB-2026-000123.</summary>
    public const string LabOrderNumber = "LAB_ORDER";
    /// <summary>Lab sample barcode number, scoped per branch and year — e.g. SMP-2026-000123.</summary>
    public const string LabSampleNumber = "LAB_SAMPLE";
    public const string PurchaseRequestNumber = "PR_NUMBER";
    public const string PurchaseOrderNumber = "PO_NUMBER";
    public const string GrnNumber = "GRN_NUMBER";
    public const string PurchaseReturnNumber = "PURCHASE_RETURN_NUMBER";
    public const string PharmacySaleNumber = "PHARMACY_SALE_NUMBER";
    public const string SalesReturnNumber = "SALES_RETURN_NUMBER";
    public const string IssueNumber = "ISSUE_NUMBER";
    public const string IssueReturnNumber = "ISSUE_RETURN_NUMBER";
    public const string StockAdjustmentNumber = "STOCK_ADJUSTMENT_NUMBER";
    public const string StockTransferNumber = "STOCK_TRANSFER_NUMBER";
    public const string StockAuditNumber = "STOCK_AUDIT_NUMBER";
    public const string IndentNumber = "INDENT_NUMBER";
    /// <summary>IP admission number, scoped per branch and year — e.g. IP-2026-000123.</summary>
    public const string AdmissionNumber = "ADMISSION_NUMBER";
    /// <summary>Radiology order number, scoped per branch and year — e.g. RAD-2026-000123.</summary>
    public const string RadOrderNumber = "RAD_ORDER";
    /// <summary>Radiology accession number (PACS placeholder), scoped per branch and year — e.g. ACC-2026-000123.</summary>
    public const string RadAccessionNumber = "RAD_ACCESSION";
    /// <summary>OT surgery booking number, scoped per branch and year — e.g. OT-2026-000123.</summary>
    public const string SurgeryBookingNumber = "SURGERY_BOOKING";
    /// <summary>ER visit number, scoped per branch and year — e.g. ER-2026-000123.</summary>
    public const string EmergencyVisitNumber = "EMERGENCY_VISIT";
    /// <summary>ER token, scoped per branch and reset daily (encoded date, same idiom as OpVisitToken).</summary>
    public const string EmergencyToken = "EMERGENCY_TOKEN";
    /// <summary>MLC number, scoped per branch and year — e.g. MLC-2026-000123.</summary>
    public const string MlcNumber = "MLC_NUMBER";
    /// <summary>MRD physical file number, scoped per branch and year — e.g. MR-2026-000123.</summary>
    public const string RecordFileNumber = "MRD_FILE_NUMBER";
    /// <summary>Journal voucher number, scoped per branch and year — e.g. JV-2026-000123.</summary>
    public const string JournalVoucherNumber = "FINANCE_JV_NUMBER";
    /// <summary>Payment voucher number, scoped per branch and year — e.g. PV-2026-000123.</summary>
    public const string PaymentVoucherNumber = "FINANCE_PV_NUMBER";
    /// <summary>Receipt voucher number, scoped per branch and year — e.g. RV-2026-000123.</summary>
    public const string ReceiptVoucherNumber = "FINANCE_RV_NUMBER";
    /// <summary>Biomedical work order number, scoped per branch and year — e.g. WO-2026-000123.</summary>
    public const string WorkOrderNumber = "BIOMED_WORK_ORDER";
    /// <summary>Biomedical breakdown complaint number, scoped per branch and year — e.g. BC-2026-000123.</summary>
    public const string ComplaintNumber = "BIOMED_COMPLAINT";
    /// <summary>Loyalty card number, scoped per branch and year — e.g. LC-2026-000123.</summary>
    public const string LoyaltyCardNumber = "LOYALTY_CARD";
    /// <summary>Insurance pre-authorization request number, scoped per branch and year — e.g. PA-2026-000123.</summary>
    public const string PreAuthNumber = "INSURANCE_PREAUTH";
    /// <summary>Insurance claim number, scoped per branch and year — e.g. CLM-2026-000123.</summary>
    public const string ClaimNumber = "INSURANCE_CLAIM";
}

/// <summary>
/// Atomic, gap-tolerant per-key/scope/year counters (UHID, IP No, Bill No, Sample No,
/// GRN No…). ScopeId is 0 for org-wide sequences; otherwise a BranchId or other id the
/// caller chooses. The "year" bucket is reused by callers that need finer-grained resets
/// (e.g. appointment tokens encode the full date as an int to reset daily per doctor).
/// </summary>
public interface ISequenceService
{
    Task<long> NextValueAsync(string sequenceKey, long scopeId, int year, CancellationToken ct = default);
}
