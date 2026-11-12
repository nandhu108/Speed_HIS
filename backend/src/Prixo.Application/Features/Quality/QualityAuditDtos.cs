using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

public record SaveAuditTemplateItemRequest(int SortOrder, string Question, decimal MaxScore);
public record SaveAuditTemplateRequest(string Code, string Name, string? Category, IReadOnlyList<SaveAuditTemplateItemRequest> Items);

public record AuditTemplateItemDto(long Id, int SortOrder, string Question, decimal MaxScore);
public record AuditTemplateDto(long Id, string Code, string Name, string? Category, IReadOnlyList<AuditTemplateItemDto> Items);

public record ScheduleAuditRequest(long TemplateId, DateOnly ScheduledDate);
public record RecordAuditResponseRequest(long TemplateItemId, decimal Score, string? Remarks);
public record ScoreAuditRequest(IReadOnlyList<RecordAuditResponseRequest> Responses);

public record AuditResponseDto(long TemplateItemId, string Question, decimal MaxScore, decimal Score, string? Remarks);

public record AuditDto(
    long Id, long TemplateId, string TemplateName, DateOnly ScheduledDate, DateOnly? ConductedDate,
    QualityAuditStatus Status, string? ConductedByName, string? Remarks, decimal? ScorePercent,
    IReadOnlyList<AuditResponseDto> Responses);

public record AuditQuery(QualityAuditStatus? Status, int Page = 1, int PageSize = 20);
