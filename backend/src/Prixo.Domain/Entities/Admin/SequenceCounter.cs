namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// Atomic per-key/scope/year counter backing SequenceService (UHID, IP No, Bill No…).
/// Not a BaseEntity — a system counter, never edited through the API or audited.
/// ScopeId is 0 for org-wide sequences (e.g. UHID); otherwise a BranchId.
/// </summary>
public class SequenceCounter
{
    public long Id { get; set; }
    public required string SequenceKey { get; set; }
    public long ScopeId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}
