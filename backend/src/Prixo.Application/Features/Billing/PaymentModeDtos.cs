namespace Prixo.Application.Features.Billing;

public record PaymentModeDto(long Id, string Code, string Name, string? Description, bool RequiresReference, bool IsActive);

public record SavePaymentModeRequest(string Code, string Name, string? Description, bool RequiresReference, bool IsActive = true);
