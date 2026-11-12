namespace Prixo.Application.Features.Reports;

public record ReportCatalogEntryDto(
    string Code, string Name, string? Description, string Category, string? LinkPath, bool IsExecutable, IReadOnlyList<ReportColumnDto> Columns);

public record ReportColumnDto(string Key, string Label);

public record ReportExecuteRequest(DateOnly? FromDate, DateOnly? ToDate, bool CrossBranch = false);

public record ReportResultDto(string ReportName, IReadOnlyList<ReportColumnDto> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);
