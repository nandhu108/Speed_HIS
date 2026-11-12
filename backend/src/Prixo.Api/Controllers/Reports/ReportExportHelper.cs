using Microsoft.AspNetCore.Mvc;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Reports;

/// <summary>CSV/Excel/PDF export dispatch shared by every report controller (Custom Report Builder, OPD/IPD reports) so the format switch lives in one place instead of being copied per controller.</summary>
public static class ReportExportHelper
{
    public static IActionResult ToFileResult(
        ReportResultDto result, string format, string fileNameBase, string? dateRangeLabel,
        IReportExportService excel, IPdfService pdf)
    {
        return format.ToLowerInvariant() switch
        {
            "csv" => new FileContentResult(ReportCsvWriter.Write(result.Columns, result.Rows), "text/csv") { FileDownloadName = $"{fileNameBase}.csv" },
            "excel" => new FileContentResult(
                excel.ToExcel(result.ReportName, [.. result.Columns.Select(c => (c.Key, c.Label))], result.Rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = $"{fileNameBase}.xlsx" },
            "pdf" => new FileContentResult(pdf.GenerateTabularReport(new TabularReportData(
                result.ReportName, dateRangeLabel,
                [.. result.Columns.Select(c => c.Label)],
                [.. result.Rows.Select(row => (IReadOnlyList<string>)[.. result.Columns.Select(c => row.GetValueOrDefault(c.Key)?.ToString() ?? "")])])),
                "application/pdf") { FileDownloadName = $"{fileNameBase}.pdf" },
            _ => throw new DomainException($"Unknown export format '{format}' — use csv, excel, or pdf."),
        };
    }

    public static string? DateRangeLabel(DateOnly? fromDate, DateOnly? toDate) =>
        fromDate is null && toDate is null ? null : $"{fromDate?.ToString("dd-MMM-yyyy") ?? "…"} to {toDate?.ToString("dd-MMM-yyyy") ?? "…"}";
}
