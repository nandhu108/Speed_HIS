using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>Reusable consent text (general treatment, surgical, data-use…) signed at registration or before a procedure.</summary>
public class ConsentTemplate : BaseEntity
{
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string BodyText { get; set; }
}
