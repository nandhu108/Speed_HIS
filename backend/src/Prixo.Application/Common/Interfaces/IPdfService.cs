namespace Prixo.Application.Common.Interfaces;

public record RegistrationCardData(
    string Uhid, string FullName, string Gender, string AgeDisplay, string Mobile,
    string? Address, string BranchName, string BranchAddress, DateTimeOffset RegisteredAt,
    byte[]? PhotoBytes);

public record LoyaltyCardData(
    string CardNumber, string MemberName, string MemberUhid, string CardTypeName, string? CategoryName,
    DateOnly IssuedDate, DateOnly ExpiryDate, string BranchName, byte[]? PhotoBytes);

public record VisitSlipData(
    string VisitNumber, int TokenNumber, string PatientName, string PatientUhid,
    string DoctorName, string BranchName, DateOnly VisitDate, string VisitType);

public record VisitSummaryData(
    string VisitNumber, string PatientName, string PatientUhid, string DoctorName, string BranchName,
    DateOnly VisitDate, IReadOnlyList<string> Diagnoses, IReadOnlyList<string> PrescriptionLines,
    string? AdviceNotes, DateOnly? FollowUpDate);

public record BillReceiptLine(string Description, decimal Quantity, decimal Rate, decimal Discount, decimal Tax, decimal Total);

public record BillReceiptData(
    string BillNumber, DateOnly BillDate, string PatientName, string PatientUhid, string BranchName,
    string BranchAddress, IReadOnlyList<BillReceiptLine> Items, decimal SubTotal, decimal DiscountAmount,
    decimal TaxAmount, decimal TotalAmount, decimal PaidAmount, decimal BalanceAmount,
    string? ReceiptNumber, string? PaymentModeName, decimal? PaymentAmount,
    /// <summary>Branch-configurable via the Billing.ReceiptFooterNote setting ("Control Panel - Billing"); falls back to a default thank-you line when null/empty.</summary>
    string? FooterNote = null);

public record BillEstimateLineData(string Description, decimal Quantity, decimal Rate, decimal Total);

public record BillEstimateData(
    string EstimateNumber, DateOnly EstimateDate, string PatientName, string PatientUhid, string BranchName,
    string BranchAddress, IReadOnlyList<BillEstimateLineData> Items, decimal TotalAmount, string? Notes);

public record LabReportValueLine(string ParameterName, string Value, string? Unit, string? RangeDisplay, bool IsAbnormal, bool IsCritical);

public record LabReportTestSection(string TestName, string? Method, IReadOnlyList<LabReportValueLine> Values, string? Notes);

public record LabReportData(
    string OrderNumber, DateOnly OrderDate, string PatientName, string PatientUhid, string PatientGender,
    string PatientAgeDisplay, string BranchName, string BranchAddress, string? ReferringDoctorName,
    IReadOnlyList<LabReportTestSection> Sections, DateTimeOffset? ApprovedAt, string? ApprovedByName);

public record PharmacyInvoiceLine(string DrugName, string BatchNumber, DateOnly ExpiryDate, decimal Quantity, decimal Rate, decimal Discount, decimal Tax, decimal Total);

public record PharmacyInvoiceData(
    string SaleNumber, DateOnly SaleDate, string? PatientName, string BranchName, string BranchAddress,
    IReadOnlyList<PharmacyInvoiceLine> Items, decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount);

public record AdmissionSlipData(
    string AdmissionNumber, string PatientName, string PatientUhid, string PatientGender, string PatientAgeDisplay,
    string BranchName, string BranchAddress, DateTimeOffset AdmissionDate, string AdmissionType, string WardName,
    string RoomName, string BedCode, string PrimaryConsultantName, decimal InitialDepositAmount);

public record DischargeSummaryPdfData(
    string AdmissionNumber, string PatientName, string PatientUhid, string PatientGender, string PatientAgeDisplay,
    string BranchName, string BranchAddress, DateTimeOffset AdmissionDate, DateTimeOffset DischargedAt,
    string PrimaryConsultantName, string FinalDiagnosis, string? CourseInHospital, string? MedicationsOnDischarge,
    string? FollowUpAdvice, string? ConditionAtDischarge, string? ApprovedByName, DateTimeOffset? ApprovedAt);

public record DischargeSlipData(
    string AdmissionNumber, string PatientName, string PatientUhid, string BranchName, DateTimeOffset AdmissionDate,
    DateTimeOffset DischargedAt, string DischargeType, string WardName, string RoomName, string BedCode,
    decimal BillTotalAmount, decimal BillBalanceAmount);

public record RadiologyReportData(
    string OrderNumber, string AccessionNumber, DateOnly OrderDate, string PatientName, string PatientUhid,
    string PatientGender, string PatientAgeDisplay, string BranchName, string BranchAddress, string? ReferringDoctorName,
    string TestName, string ModalityName, string BodyPart, string Findings, string Impression,
    string? ApprovedByName, DateTimeOffset? ApprovedAt);

/// <summary>Generic tabular report PDF (plan §17/§23: Custom Report Builder "export CSV/PDF/Excel") — one column-headers-plus-rows layout shared by every report_definitions row, rather than a bespoke template per report.</summary>
public record TabularReportData(string Title, string? DateRangeLabel, IReadOnlyList<string> ColumnLabels, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>PDF generation for printable documents (registration card, OPD visit slip/summary, bill receipt, lab report, pharmacy invoice, IPD admission/discharge documents, radiology report, generic tabular report).</summary>
public interface IPdfService
{
    byte[] GenerateRegistrationCard(RegistrationCardData data);
    byte[] GenerateLoyaltyCard(LoyaltyCardData data);
    byte[] GenerateVisitSlip(VisitSlipData data);
    byte[] GenerateVisitSummary(VisitSummaryData data);
    byte[] GenerateBillReceipt(BillReceiptData data);
    byte[] GenerateBillEstimate(BillEstimateData data);
    byte[] GenerateLabReport(LabReportData data);
    byte[] GeneratePharmacyInvoice(PharmacyInvoiceData data);
    byte[] GenerateAdmissionSlip(AdmissionSlipData data);
    byte[] GenerateDischargeSummaryPdf(DischargeSummaryPdfData data);
    byte[] GenerateDischargeSlip(DischargeSlipData data);
    byte[] GenerateRadiologyReport(RadiologyReportData data);
    byte[] GenerateTabularReport(TabularReportData data);
}
