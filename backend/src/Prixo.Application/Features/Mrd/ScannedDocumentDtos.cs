using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public record ScannedDocumentDto(
    long Id, long AdmissionId, long DocumentTypeId, string DocumentTypeName, string FileName,
    string ContentType, long SizeBytes, string? Description, VirusScanStatus VirusScanStatus,
    string UploadedByName, DateTimeOffset UploadedAt);
