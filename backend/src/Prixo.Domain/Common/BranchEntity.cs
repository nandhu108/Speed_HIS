namespace Prixo.Domain.Common;

/// <summary>
/// Base for branch-scoped entities. A global query filter restricts reads to the
/// current user's branch; cross-branch access requires IgnoreQueryFilters plus the
/// REPORT.CROSSBRANCH.VIEW permission.
/// </summary>
public abstract class BranchEntity : BaseEntity, IBranchScoped
{
    public long BranchId { get; set; }
}
