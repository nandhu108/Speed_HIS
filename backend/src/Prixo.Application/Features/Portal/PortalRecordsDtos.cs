namespace Prixo.Application.Features.Portal;

public record PortalLabOrderDto(long Id, string OrderNumber, DateOnly OrderDate, bool HasApprovedReport);
public record PortalRadOrderItemDto(long Id, string AccessionNumber, string TestName, DateOnly OrderDate, bool IsApproved);
public record PortalPrescriptionDto(long Id, DateOnly VisitDate, string DoctorName, IReadOnlyList<string> Items);
public record PortalBillDto(long Id, string BillNumber, DateOnly BillDate, decimal TotalAmount, decimal BalanceAmount, string Status);
public record PortalDischargeSummaryDto(long AdmissionId, string AdmissionNumber, DateTimeOffset DischargedAt, string FinalDiagnosis);
public record OnlinePaymentRequestedDto(long RequestId, decimal Amount, DateTimeOffset RequestedAt);
