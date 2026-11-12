namespace Prixo.Application.Features.Rad;

public record RadiologyTestListItemDto(
    long Id, string Code, string Name, long ModalityId, string ModalityName, string BodyPart,
    bool RequiresContrast, int TatHours, bool IsActive);

public record RadiologyTestDetailDto(
    long Id, string Code, string Name, long ModalityId, string ModalityName, string BodyPart, bool RequiresContrast,
    string? PrepInstructions, long? ServiceId, string? ServiceName, int TatHours, bool IsActive);

public record SaveRadiologyTestRequest(
    string Code, string Name, long ModalityId, string BodyPart, bool RequiresContrast, string? PrepInstructions,
    long? ServiceId, int TatHours, bool IsActive = true);

public record RadReportTemplateListItemDto(long Id, long RadiologyTestId, string RadiologyTestName, string Name, bool IsActive);

public record RadReportTemplateDetailDto(
    long Id, long RadiologyTestId, string RadiologyTestName, string Name, string FindingsTemplate,
    string ImpressionTemplate, bool IsActive);

public record SaveRadReportTemplateRequest(
    long RadiologyTestId, string Name, string FindingsTemplate, string ImpressionTemplate, bool IsActive = true);
