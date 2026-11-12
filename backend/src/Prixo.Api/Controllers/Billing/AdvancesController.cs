using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/advances")]
public class AdvancesController(AdvanceService advances, BillingService billing) : ApiControllerBase
{
    [HttpPost]
    [HasPermission(PermissionRegistry.AdvanceCollect)]
    public async Task<ActionResult<ApiResponse<long>>> Collect(CollectAdvanceRequest request, CancellationToken ct)
    {
        var id = await advances.CollectAsync(request, ct);
        return CreatedResponse($"/api/v1/billing/advances/{id}", id, "Advance collected.");
    }

    [HttpGet("patient/{patientId:long}")]
    [HasPermission(PermissionRegistry.AdvanceView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdvanceDto>>>> GetByPatient(long patientId, CancellationToken ct) =>
        OkResponse(await advances.GetByPatientAsync(patientId, ct));

    [HttpPost("{id:long}/refunds")]
    [HasPermission(PermissionRegistry.RefundRequest)]
    public async Task<ActionResult<ApiResponse<RefundDto>>> RequestRefund(long id, RequestAdvanceRefundRequest request, CancellationToken ct)
    {
        var refund = await billing.RequestAdvanceRefundAsync(id, request, ct);
        return CreatedResponse($"/api/v1/billing/refunds/{refund.Id}", refund, "Refund requested.");
    }

    [HttpPost("{id:long}/transfer")]
    [HasPermission(PermissionRegistry.AdvanceTransfer)]
    public async Task<ActionResult<ApiResponse<long>>> Transfer(long id, TransferAdvanceRequest request, CancellationToken ct)
    {
        var newAdvanceId = await advances.TransferAsync(id, request, ct);
        return CreatedResponse($"/api/v1/billing/advances/{newAdvanceId}", newAdvanceId, "Advance transferred.");
    }
}
