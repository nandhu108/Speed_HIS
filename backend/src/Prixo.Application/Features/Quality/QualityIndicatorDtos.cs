using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

public record SaveIndicatorRequest(
    string Code, string Name, string? Description, string? Category, string NumeratorDefinition,
    string DenominatorDefinition, string? Unit, decimal? TargetValue, QualityAutoComputeSource? AutoComputeSource);

public record IndicatorDto(
    long Id, string Code, string Name, string? Description, string? Category, string NumeratorDefinition,
    string DenominatorDefinition, string? Unit, decimal? TargetValue, QualityAutoComputeSource? AutoComputeSource);

public record RecordIndicatorEntryRequest(DateOnly PeriodMonth, decimal Numerator, decimal Denominator, string? Remarks);

public record IndicatorEntryDto(
    long Id, long IndicatorId, DateOnly PeriodMonth, decimal Numerator, decimal Denominator,
    decimal? ComputedValue, bool IsAutoComputed, string? Remarks, string EnteredByName);
