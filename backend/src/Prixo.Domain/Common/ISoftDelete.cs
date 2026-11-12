namespace Prixo.Domain.Common;

/// <summary>
/// Entities implementing this are never hard-deleted: the interceptor converts
/// deletes to IsDeleted = true, and a global query filter hides deleted rows.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
