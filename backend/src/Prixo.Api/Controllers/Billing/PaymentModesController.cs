using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/payment-modes")]
public class PaymentModesController(PaymentModeService paymentModes) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PaymentModeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentModeDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await paymentModes.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PaymentModeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SavePaymentModeRequest request, CancellationToken ct)
    {
        var id = await paymentModes.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/billing/payment-modes/{id}", id, "Payment mode created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PaymentModeManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SavePaymentModeRequest request, CancellationToken ct)
    {
        await paymentModes.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Payment mode updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.PaymentModeManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await paymentModes.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Payment mode deleted.");
    }
}
