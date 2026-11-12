using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Lab;

public record ReferenceRangeDto(
    long Id, Gender? Sex, int? MinAgeYears, int? MaxAgeYears,
    decimal? NormalLow, decimal? NormalHigh, decimal? CriticalLow, decimal? CriticalHigh, string? TextNormalValue);

public record SaveReferenceRangeRequest(
    Gender? Sex, int? MinAgeYears, int? MaxAgeYears,
    decimal? NormalLow, decimal? NormalHigh, decimal? CriticalLow, decimal? CriticalHigh, string? TextNormalValue);

public record LabTestParameterDto(
    long Id, string Code, string Name, string? Unit, ParameterValueType ValueType,
    string? DropdownOptions, int SortOrder, IReadOnlyList<ReferenceRangeDto> ReferenceRanges);

public record SaveLabTestParameterRequest(
    string Code, string Name, string? Unit, ParameterValueType ValueType, string? DropdownOptions,
    int SortOrder, IReadOnlyList<SaveReferenceRangeRequest> ReferenceRanges);

public record LabTestListItemDto(
    long Id, string Code, string Name, string? DepartmentName, string SampleTypeName,
    int TatHours, bool IsPanel, bool IsActive);

public record LabTestDetailDto(
    long Id, string Code, string Name, long? DepartmentId, string? DepartmentName,
    long SampleTypeId, string SampleTypeName, long? ServiceId, string? ServiceName,
    string? Method, int TatHours, bool IsPanel, bool IsActive, IReadOnlyList<LabTestParameterDto> Parameters);

public record SaveLabTestRequest(
    string Code, string Name, long? DepartmentId, long SampleTypeId, long? ServiceId, string? Method,
    int TatHours, bool IsPanel, IReadOnlyList<SaveLabTestParameterRequest> Parameters, bool IsActive = true);

public record TestProfileListItemDto(long Id, string Code, string Name, decimal? Price, int TestCount, bool IsActive);

public record TestProfileMemberDto(long LabTestId, string LabTestName);

public record TestProfileDetailDto(
    long Id, string Code, string Name, decimal? Price, bool IsActive, IReadOnlyList<TestProfileMemberDto> Tests);

public record SaveTestProfileRequest(
    string Code, string Name, decimal? Price, IReadOnlyList<long> LabTestIds, bool IsActive = true);
