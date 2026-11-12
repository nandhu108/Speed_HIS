using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/evidence")]
public class EvidenceController(QualityEvidenceService evidence) : ApiControllerBase
{
    private const long MaxUploadRequestBytes = 21 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.QualityEvidenceView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceDto>>>> GetAll([FromQuery] EvidenceQuery query, CancellationToken ct) =>
        OkResponse(await evidence.GetAllAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityEvidenceUpload)]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> Upload(
        [FromForm] long standardId, [FromForm] string title, [FromForm] string? description, IFormFile file, CancellationToken ct)
    {
        var request = new UploadEvidenceRequest(standardId, title, description);
        await using var stream = file.OpenReadStream();
        var id = await evidence.UploadAsync(request, stream, file.FileName, ct);
        return CreatedResponse($"/api/v1/quality/evidence/{id}", id, "Evidence uploaded.");
    }

    [HttpGet("{id:long}/file")]
    [HasPermission(PermissionRegistry.QualityEvidenceView)]
    public async Task<IActionResult> DownloadFile(long id, CancellationToken ct)
    {
        var (content, fileName) = await evidence.OpenAsync(id, ct);
        return File(content, "application/octet-stream", fileName);
    }
}
