namespace Prixo.Application.Features.Finance;

/// <summary>
/// Stable codes for the handful of accounts the seeder creates and the auto-posting rules
/// (BillingService's payment hook, Payable/Receivable settlement) look up by code — never by
/// Id, per CLAUDE.md's "seed data is idempotent code keyed by stable codes".
/// </summary>
public static class SystemAccountCodes
{
    public const string PatientRevenue = "4000";
    public const string AccountsPayable = "2000";
    public const string AccountsReceivable = "1100";
}
