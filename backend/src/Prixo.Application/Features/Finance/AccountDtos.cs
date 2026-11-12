using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Finance;

public record SaveAccountRequest(string Code, string Name, AccountType AccountType, long? ParentAccountId, string? Description);

public record AccountDto(long Id, string Code, string Name, AccountType AccountType, long? ParentAccountId, string? ParentAccountName, bool IsSystemAccount, string? Description, bool IsActive);
