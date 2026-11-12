using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Biomed;

public record SaveAmcContractRequest(
    long EquipmentId, long VendorId, ContractType ContractType, string ContractNumber,
    DateOnly StartDate, DateOnly EndDate, decimal Cost, int RenewalReminderDays, string? Notes);

public record AmcContractDto(
    long Id, long EquipmentId, string EquipmentName, long VendorId, string VendorName, ContractType ContractType,
    string ContractNumber, DateOnly StartDate, DateOnly EndDate, decimal Cost, int RenewalReminderDays, bool IsRenewalDue, string? Notes);
