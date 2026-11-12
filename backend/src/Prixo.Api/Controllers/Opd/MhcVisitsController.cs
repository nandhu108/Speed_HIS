using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Opd;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Opd;

/// <summary>Front-office MHC (Master Health Checkup) registration — a deliberately narrow
/// surface over OpVisitService's shared engine (register/list/cancel/slip only; no consultation
/// workspace exists for MHC in this pass, see docs/modules/opd.md).</summary>
[Route("api/v1/mhc/visits")]
public class MhcVisitsController(OpVisitService visits) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpVisitListItemDto>>>> GetPaged(
        [FromQuery] OpVisitQuery query, CancellationToken ct)
    {
        query.Category = VisitCategory.Mhc;
        return OkResponse(await visits.GetPagedAsync(query, ct));
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<OpVisitDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await visits.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MhcVisitCreate)]
    public async Task<ActionResult<ApiResponse<OpVisitDetailDto>>> Register(
        RegisterMhcVisitRequest request, CancellationToken ct)
    {
        var result = await visits.RegisterMhcAsync(request, ct);
        return CreatedResponse($"/api/v1/mhc/visits/{result.Id}", result, $"MHC visit registered — token #{result.TokenNumber}.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.MhcVisitManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelVisitRequest request, CancellationToken ct)
    {
        await visits.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Visit cancelled.");
    }

    [HttpGet("{id:long}/slip")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> GetSlip(long id, CancellationToken ct)
    {
        var pdf = await visits.GenerateVisitSlipAsync(id, ct);
        return File(pdf, "application/pdf", $"mhc-visit-slip-{id}.pdf");
    }

    private const long MaxImportBytes = 5 * 1024 * 1024;

    /// <summary>"MHC Bulk Import" — one MHC doctor/package for the whole batch, a CSV of UHIDs per row.</summary>
    [HttpPost("import")]
    [HasPermission(PermissionRegistry.MhcVisitImport)]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<ActionResult<ApiResponse<BulkImportResultDto>>> Import(
        [FromForm] long mhcDoctorId, [FromForm] long mhcPackageId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }
        await using var stream = file.OpenReadStream();
        var result = await visits.ImportMhcVisitsAsync(mhcDoctorId, mhcPackageId, stream, ct);
        return OkResponse(result, $"Import complete: {result.Inserted} registered, {result.Skipped} skipped.");
    }
}
