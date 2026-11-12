using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/discount-approvals")]
public class DiscountApprovalsController(BillingService billing) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DiscountApprove)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscountApprovalDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await billing.GetPendingDiscountApprovalsAsync(ct));

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.DiscountApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecideDiscountRequest request, CancellationToken ct)
    {
        await billing.DecideDiscountAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Discount approved." : "Discount rejected.");
    }
}
