using System.Text.Json;

namespace Prixo.Application.Features.Reports;

/// <summary>ColumnsJson is hand-written lower-camelCase in the seed data; System.Text.Json's default Deserialize is case-sensitive, so both readers here need this explicitly.</summary>
internal static class ReportJsonOptions
{
    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
}
