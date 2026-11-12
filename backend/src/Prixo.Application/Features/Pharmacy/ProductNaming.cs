using Prixo.Domain.Entities.Pharmacy;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// Display name for a Batch/line that carries either a Drug or an Item (Phase 13's
/// exactly-one-of pattern) — only safe to use against an already-materialized entity graph
/// (post-Include, in-memory LINQ), never inside an IQueryable still being translated to SQL.
/// </summary>
internal static class ProductNaming
{
    public static string Of(Batch batch) => batch.Drug?.Name ?? batch.Item?.Name ?? string.Empty;
}
