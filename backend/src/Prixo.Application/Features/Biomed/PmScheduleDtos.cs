namespace Prixo.Application.Features.Biomed;

public record SavePmScheduleRequest(long EquipmentId, int FrequencyDays, DateOnly NextDueDate, string? ChecklistNotes);

public record PmScheduleDto(
    long Id, long EquipmentId, string EquipmentName, int FrequencyDays, DateOnly NextDueDate,
    DateOnly? LastGeneratedDate, bool IsDue, string? ChecklistNotes);
