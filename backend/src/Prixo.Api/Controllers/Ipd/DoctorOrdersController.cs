using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/admissions/{admissionId:long}/orders")]
public class DoctorOrdersController(DoctorOrderService orders) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DoctorOrderView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorOrderDto>>>> GetByAdmission(
        long admissionId, [FromQuery] DoctorOrderType? orderType, CancellationToken ct) =>
        OkResponse(await orders.GetByAdmissionAsync(admissionId, orderType, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DoctorOrderCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(long admissionId, CreateDoctorOrderRequest request, CancellationToken ct)
    {
        var id = await orders.CreateAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/orders/{id}", id, "Order placed.");
    }

    [HttpPost("{orderId:long}/cancel")]
    [HasPermission(PermissionRegistry.DoctorOrderCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long admissionId, long orderId, CancelDoctorOrderRequest request, CancellationToken ct)
    {
        await orders.CancelAsync(admissionId, orderId, request, ct);
        return OkResponse<object?>(null, "Order cancelled.");
    }
}
