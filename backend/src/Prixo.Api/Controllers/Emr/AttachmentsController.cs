using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Emr;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Emr;

[Route("api/v1/emr/patients/{patientId:long}/attachments")]
public class AttachmentsController(AttachmentService attachments) : ApiControllerBase
{
    private const long MaxAttachmentRequestBytes = 21 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.EmrAttachmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttachmentDto>>>> Get(long patientId, CancellationToken ct) =>
        OkResponse(await attachments.GetAsync(patientId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.EmrAttachmentManage)]
    [RequestSizeLimit(MaxAttachmentRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> Upload(
        long patientId, [FromForm] string attachmentType, [FromForm] string? description, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var id = await attachments.UploadAsync(patientId, attachmentType, stream, file.FileName, file.ContentType, file.Length, description, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/attachments/{id}", id, "Attachment uploaded.");
    }

    [HttpGet("{attachmentId:long}/download")]
    [HasPermission(PermissionRegistry.EmrAttachmentView)]
    public async Task<IActionResult> Download(long patientId, long attachmentId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await attachments.OpenAsync(patientId, attachmentId, ct);
        return File(content, contentType, fileName);
    }
}
