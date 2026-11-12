namespace Prixo.Application.Features.Finance;

public record LedgerEntryRowDto(long Id, DateOnly EntryDate, string VoucherNumber, string Narration, decimal DebitAmount, decimal CreditAmount, decimal RunningBalance);

public record AccountLedgerDto(long AccountId, string AccountCode, string AccountName, decimal OpeningBalance, IReadOnlyList<LedgerEntryRowDto> Entries, decimal ClosingBalance);
