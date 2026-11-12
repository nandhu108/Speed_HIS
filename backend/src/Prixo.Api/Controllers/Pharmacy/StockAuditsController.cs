using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/audits")]
public class StockAuditsController(StockService stock) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.StockAudit)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockAuditListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await stock.GetAuditsAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.StockAudit)]
    public async Task<ActionResult<ApiResponse<StockAuditDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await stock.GetAuditByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.StockAudit)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateStockAuditRequest request, CancellationToken ct)
    {
        var id = await stock.CreateAuditAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/audits/{id}", id, "Stock audit started.");
    }

    [HttpPost("{id:long}/counts")]
    [HasPermission(PermissionRegistry.StockAudit)]
    public async Task<ActionResult<ApiResponse>> RecordCount(long id, AddAuditCountRequest request, CancellationToken ct)
    {
        await stock.RecordCountAsync(id, request, ct);
        return OkResponse<object?>(null, "Count recorded.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.StockAudit)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await stock.CompleteAuditAsync(id, ct);
        return OkResponse<object?>(null, "Stock audit completed — variances posted.");
    }
}
