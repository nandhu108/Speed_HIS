using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>MRD document-type master (plan §17: "document-type master") — categorizes scanned documents (Discharge Summary, Consent Form...). Global — shared across branches.</summary>
public class RecordDocumentType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
