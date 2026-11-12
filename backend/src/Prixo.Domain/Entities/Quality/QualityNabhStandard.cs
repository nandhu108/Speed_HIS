using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Quality;

/// <summary>
/// NABH chapter/standard reference — organizes the evidence library and non-conformance
/// findings. Simple master, plugging into the generic SimpleMasterService/Controller framework
/// unchanged (whose shared code validator only allows [A-Z0-9_-]). Convention: Code is
/// "&lt;CHAPTER&gt;_&lt;STANDARD&gt;" (e.g. "AAC_1") — the chapter is the substring before the
/// first '_', derived on read (QualityMath.ChapterOf), not a separate stored column, so this
/// master needs no fields beyond Code/Name/Description.
/// </summary>
public class QualityNabhStandard : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
