using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/estimates")]
public class BillEstimatesController(BillEstimateService estimates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BillEstimateView)]
    public async Task<ActionResult<ApiResponse<PagedResult<BillEstimateListItemDto>>>> GetPaged(
        [FromQuery] BillEstimateQuery query, CancellationToken ct) =>
        OkResponse(await estimates.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BillEstimateView)]
    public async Task<ActionResult<ApiResponse<BillEstimateDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await estimates.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BillEstimateCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateBillEstimateRequest request, CancellationToken ct)
    {
        var id = await estimates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/billing/estimates/{id}", id, "Estimate created.");
    }

    [HttpPost("{id:long}/copy")]
    [HasPermission(PermissionRegistry.BillEstimateCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Copy(long id, CancellationToken ct)
    {
        var newId = await estimates.CopyAsync(id, ct);
        return CreatedResponse($"/api/v1/billing/estimates/{newId}", newId, "Estimate copied.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.BillEstimateCreate)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await estimates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Estimate deleted.");
    }

    [HttpGet("{id:long}/pdf")]
    [HasPermission(PermissionRegistry.BillEstimateView)]
    public async Task<IActionResult> GetPdf(long id, CancellationToken ct)
    {
        var pdf = await estimates.GeneratePdfAsync(id, ct);
        return File(pdf, "application/pdf", $"bill-estimate-{id}.pdf");
    }
}
