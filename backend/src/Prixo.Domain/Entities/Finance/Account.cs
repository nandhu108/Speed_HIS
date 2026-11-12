using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Finance;

/// <summary>
/// Chart of Accounts (plan §18). Global — one CoA shared across every branch, same posture
/// as the Service/Drug catalogs (CLAUDE.md: catalogs are global, transactional data is
/// branch-scoped). IsSystemAccount marks the handful of accounts the seeder creates and the
/// auto-posting rules (BillingService's payment posting, Payable/Receivable settlement) look
/// up by Code — protected from deletion.
/// </summary>
public class Account : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public AccountType AccountType { get; set; }
    public long? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public bool IsSystemAccount { get; set; }
    public string? Description { get; set; }
}
