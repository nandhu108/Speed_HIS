using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Quality;

/// <summary>Simple master (e.g. Quality Assurance Committee, Infection Control Committee) — plugs into the generic SimpleMasterService/SimpleMasterControllerBase framework.</summary>
public class QualityCommittee : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
