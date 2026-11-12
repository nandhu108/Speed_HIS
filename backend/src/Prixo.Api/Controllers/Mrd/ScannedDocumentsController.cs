using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/admissions/{admissionId:long}/documents")]
public class ScannedDocumentsController(ScannedDocumentService documents) : ApiControllerBase
{
    private const long MaxDocumentRequestBytes = 21 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.MrdDocumentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScannedDocumentDto>>>> Get(long admissionId, CancellationToken ct) =>
        OkResponse(await documents.GetByAdmissionAsync(admissionId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MrdDocumentUpload)]
    [RequestSizeLimit(MaxDocumentRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> Upload(
        long admissionId, [FromForm] long documentTypeId, [FromForm] string? description, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var id = await documents.UploadAsync(admissionId, documentTypeId, stream, file.FileName, file.ContentType, file.Length, description, ct);
        return CreatedResponse($"/api/v1/mrd/admissions/{admissionId}/documents/{id}", id, "Document uploaded.");
    }

    [HttpGet("{documentId:long}/download")]
    [HasPermission(PermissionRegistry.MrdDocumentView)]
    public async Task<IActionResult> Download(long admissionId, long documentId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await documents.OpenAsync(admissionId, documentId, ct);
        return File(content, contentType, fileName);
    }
}
