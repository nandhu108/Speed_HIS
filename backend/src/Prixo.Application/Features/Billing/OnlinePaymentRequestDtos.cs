using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public record OnlinePaymentRequestDto(
    long Id, long BillId, string BillNumber, long PatientId, string PatientName,
    decimal Amount, OnlinePaymentRequestStatus Status, DateTimeOffset RequestedAt, DateTimeOffset? ProcessedAt);
