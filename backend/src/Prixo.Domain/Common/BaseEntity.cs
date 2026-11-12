namespace Prixo.Domain.Common;

/// <summary>
/// Base for every business entity. Audit columns are stamped automatically by the
/// persistence layer (AuditSaveChangesInterceptor); services never set them by hand.
/// </summary>
public abstract class BaseEntity : IAuditable, ISoftDelete
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
