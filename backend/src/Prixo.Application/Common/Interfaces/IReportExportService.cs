namespace Prixo.Application.Common.Interfaces;

/// <summary>Excel export for the Custom Report Builder (plan §17/§23: "export CSV/PDF/Excel"). CSV needs no library (plain text) and PDF reuses IPdfService's GenerateTabularReport — this is the one export format that needs a real library (ClosedXML), so it gets its own small Infrastructure-backed interface.</summary>
public interface IReportExportService
{
    byte[] ToExcel(string title, IReadOnlyList<(string Key, string Label)> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);
}
