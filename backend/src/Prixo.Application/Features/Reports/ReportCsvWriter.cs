using System.Text;

namespace Prixo.Application.Features.Reports;

/// <summary>Plain-text CSV export for the Custom Report Builder — no library needed, unlike Excel/PDF.</summary>
public static class ReportCsvWriter
{
    public static byte[] Write(IReadOnlyList<ReportColumnDto> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns.Select(c => Escape(c.Label))));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', columns.Select(c => Escape(row.GetValueOrDefault(c.Key)?.ToString() ?? ""))));
        }
        return new UTF8Encoding(true).GetBytes(sb.ToString());
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
