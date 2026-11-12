using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Insurance;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Insurance;

[Route("api/v1/insurance/claims")]
public class ClaimsController(ClaimService claims) : ApiControllerBase
{
    private const long MaxDocumentRequestBytes = 15 * 1024 * 1024 + 10_000;

    [HttpGet("pending")]
    [HasPermission(PermissionRegistry.InsuranceClaimView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClaimDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await claims.GetPendingAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.InsuranceClaimView)]
    public async Task<ActionResult<ApiResponse<ClaimDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await claims.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.InsuranceClaimCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Submit(SubmitClaimRequest request, CancellationToken ct)
    {
        var id = await claims.SubmitAsync(request, ct);
        return CreatedResponse($"/api/v1/insurance/claims/{id}", id, "Claim submitted.");
    }

    [HttpPost("{id:long}/status")]
    [HasPermission(PermissionRegistry.InsuranceClaimManage)]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(long id, UpdateClaimStatusRequest request, CancellationToken ct)
    {
        await claims.UpdateStatusAsync(id, request, ct);
        return OkResponse<object?>(null, $"Claim moved to {request.Status}.");
    }

    [HttpPost("{id:long}/settle")]
    [HasPermission(PermissionRegistry.InsuranceClaimSettle)]
    public async Task<ActionResult<ApiResponse>> Settle(long id, RecordClaimSettlementRequest request, CancellationToken ct)
    {
        await claims.RecordSettlementAsync(id, request, ct);
        return OkResponse<object?>(null, "Claim settled.");
    }

    [HttpPost("{id:long}/documents")]
    [HasPermission(PermissionRegistry.InsuranceClaimCreate)]
    [RequestSizeLimit(MaxDocumentRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> AddDocument(
        long id, [FromForm] AddClaimDocumentRequest request, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var documentId = await claims.AddDocumentAsync(
            id, request, stream, file.FileName, file.ContentType, file.Length, ct);
        return CreatedResponse($"/api/v1/insurance/claims/{id}/documents/{documentId}", documentId, "Document uploaded.");
    }

    [HttpGet("{id:long}/documents")]
    [HasPermission(PermissionRegistry.InsuranceClaimView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClaimDocumentDto>>>> GetDocuments(
        long id, CancellationToken ct) =>
        OkResponse(await claims.GetDocumentsAsync(id, ct));

    [HttpGet("{id:long}/documents/{documentId:long}/download")]
    [HasPermission(PermissionRegistry.InsuranceClaimView)]
    public async Task<IActionResult> DownloadDocument(long id, long documentId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await claims.OpenDocumentAsync(id, documentId, ct);
        return File(content, contentType, fileName);
    }
}
