using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Billing;

/// <summary>Staff worklist over the Patient Portal's "Pay Online" request outbox — see OnlinePaymentRequestService.</summary>
[Route("api/v1/billing/online-payment-requests")]
public class OnlinePaymentRequestsController(OnlinePaymentRequestService requests) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.OnlinePaymentRequestView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OnlinePaymentRequestDto>>>> Get([FromQuery] OnlinePaymentRequestStatus? status, CancellationToken ct) =>
        OkResponse(await requests.GetAsync(status, ct));

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.OnlinePaymentRequestManage)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await requests.CompleteAsync(id, ct);
        return OkResponse<object?>(null, "Marked completed.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.OnlinePaymentRequestManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancellationToken ct)
    {
        await requests.CancelAsync(id, ct);
        return OkResponse<object?>(null, "Request cancelled.");
    }
}
