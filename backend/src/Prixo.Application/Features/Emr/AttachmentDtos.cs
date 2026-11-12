using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Emr;

public record AttachmentDto(
    long Id, string AttachmentType, string FileName, string ContentType, long SizeBytes, string? Description,
    VirusScanStatus VirusScanStatus, string UploadedByName, DateTimeOffset UploadedAt);
