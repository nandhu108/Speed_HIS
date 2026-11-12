using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/refunds")]
public class RefundsController(BillingService billing) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RefundApprove)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RefundDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await billing.GetPendingRefundsAsync(ct));

    /// <summary>Approved-but-not-yet-paid-out refunds — the Issue queue.</summary>
    [HttpGet("issuable")]
    [HasPermission(PermissionRegistry.RefundIssue)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RefundDto>>>> GetIssuable(CancellationToken ct) =>
        OkResponse(await billing.GetIssuableRefundsAsync(ct));

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.RefundApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecideRefundRequest request, CancellationToken ct)
    {
        await billing.DecideRefundAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Refund approved." : "Refund rejected.");
    }

    /// <summary>Pays out an already-approved refund — a separate step from Decide/Approve.</summary>
    [HttpPost("{id:long}/issue")]
    [HasPermission(PermissionRegistry.RefundIssue)]
    public async Task<ActionResult<ApiResponse>> Issue(long id, CancellationToken ct)
    {
        await billing.IssueRefundAsync(id, ct);
        return OkResponse<object?>(null, "Refund issued.");
    }
}
