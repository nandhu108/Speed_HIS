using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record MedicationOrderRequest(long DrugId, string Dose, string Route, string Frequency, DateOnly StartDate, DateOnly? EndDate, string? Instructions);
public record DietOrderRequest(string DietType, string? Instructions);

public record CreateDoctorOrderRequest(
    long OrderedByDoctorId, DoctorOrderType OrderType, string Description, ClinicalOrderPriority Priority, string? Notes,
    MedicationOrderRequest? Medication, DietOrderRequest? Diet);

public record MedicationOrderDto(long DrugId, string DrugName, string Dose, string Route, string Frequency, DateOnly StartDate, DateOnly? EndDate, string? Instructions);
public record DietOrderDto(string DietType, string? Instructions);

public record DoctorOrderDto(
    long Id, long AdmissionId, DoctorOrderType OrderType, long OrderedByDoctorId, string OrderedByDoctorName,
    DateTimeOffset OrderedAt, ClinicalOrderPriority Priority, ClinicalOrderStatus Status, string Description,
    string? Notes, MedicationOrderDto? Medication, DietOrderDto? Diet);

public record CancelDoctorOrderRequest(string Reason);
