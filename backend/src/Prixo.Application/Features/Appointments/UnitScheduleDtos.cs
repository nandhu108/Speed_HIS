namespace Prixo.Application.Features.Appointments;

public record UnitScheduleBlockDto(long Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record SaveUnitScheduleBlockRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record ReplaceUnitScheduleRequest(IReadOnlyList<SaveUnitScheduleBlockRequest> Blocks);

public record UnitScheduleDto(long UnitId, string UnitName, IReadOnlyList<UnitScheduleBlockDto> Blocks);
