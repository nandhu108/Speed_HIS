using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

public record MarAdministrationDto(
    MarStatus Action, DateTimeOffset ActionAt, string ActionByName, string? VerifiedByName, string? Reason, string? Notes,
    long? BatchId, string? BatchNumber, decimal? Quantity, bool StockDeducted);

public record MarScheduleDto(
    long Id, long MedicationOrderId, long DrugId, string DrugName, string Dose, string Route, string Frequency,
    DateTimeOffset DueAt, MarStatus Status, bool IsHighAlert, MarAdministrationDto? Administration);

/// <summary>
/// BatchId/Quantity are optional even when Action == Administered — a nurse can always record a
/// dose with no stock effect (clinical charting is never gated on picking a batch). When both are
/// supplied, MarService decrements that batch's stock and blocks only if it lacks enough on hand
/// (same "explicit choice, honest shortfall" posture as Pharmacy's own sale/issue validation).
/// </summary>
public record MarActionRequest(MarStatus Action, string? Reason, string? Notes, long? VerifiedByUserId, long? BatchId, decimal? Quantity);
