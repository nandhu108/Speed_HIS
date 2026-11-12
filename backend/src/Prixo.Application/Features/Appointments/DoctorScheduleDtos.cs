namespace Prixo.Application.Features.Appointments;

public record DoctorScheduleBlockDto(long Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int SlotMinutes);
public record SaveDoctorScheduleBlockRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int SlotMinutes);
public record ReplaceDoctorScheduleRequest(IReadOnlyList<SaveDoctorScheduleBlockRequest> Blocks);

public record ScheduleOverrideDto(long Id, DateOnly Date, bool IsAvailable,
    TimeOnly? StartTime, TimeOnly? EndTime, int? SlotMinutes, string? Reason);
public record SaveScheduleOverrideRequest(DateOnly Date, bool IsAvailable,
    TimeOnly? StartTime, TimeOnly? EndTime, int? SlotMinutes, string? Reason);

public record DoctorScheduleDto(long DoctorId, string DoctorName,
    IReadOnlyList<DoctorScheduleBlockDto> Blocks, IReadOnlyList<ScheduleOverrideDto> Overrides);
