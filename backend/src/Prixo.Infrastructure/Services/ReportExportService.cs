using ClosedXML.Excel;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Infrastructure.Services;

/// <summary>Excel export for the Custom Report Builder, via ClosedXML (MIT) — the one export format in plan §17's "CSV/PDF/Excel" that needs a real library; CSV is plain text (ReportCsvWriter) and PDF reuses IPdfService/QuestPDF.</summary>
public class ReportExportService : IReportExportService
{
    public byte[] ToExcel(string title, IReadOnlyList<(string Key, string Label)> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(title.Length > 31 ? title[..31] : title);

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = columns[i].Label;
            cell.Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var value = rows[r].GetValueOrDefault(columns[c].Key);
                sheet.Cell(r + 2, c + 1).Value = value switch
                {
                    null => XLCellValue.FromObject(""),
                    decimal d => XLCellValue.FromObject(d),
                    int i => XLCellValue.FromObject(i),
                    _ => XLCellValue.FromObject(value.ToString()),
                };
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
