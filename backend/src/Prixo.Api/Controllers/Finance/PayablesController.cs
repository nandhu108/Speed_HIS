using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/payables")]
public class PayablesController(PayableService payables) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.FinancePayableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayableDto>>>> GetAll([FromQuery] PayableQuery query, CancellationToken ct) =>
        OkResponse(await payables.GetAllAsync(query, ct));

    [HttpGet("pending-grns")]
    [HasPermission(PermissionRegistry.FinancePayableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingGrnDto>>>> GetPendingGrns(CancellationToken ct) =>
        OkResponse(await payables.GetPendingGrnsAsync(ct));

    [HttpGet("aging")]
    [HasPermission(PermissionRegistry.FinancePayableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgingRowDto>>>> GetAging([FromQuery] DateOnly asOf, CancellationToken ct) =>
        OkResponse(await payables.GetAgingAsync(asOf, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.FinancePayableManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreatePayableRequest request, CancellationToken ct)
    {
        var id = await payables.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/finance/payables/{id}", id, "Payable raised.");
    }

    [HttpPost("{id:long}/pay")]
    [HasPermission(PermissionRegistry.FinancePayableManage)]
    public async Task<ActionResult<ApiResponse>> Pay(long id, RecordPayablePaymentRequest request, CancellationToken ct)
    {
        await payables.RecordPaymentAsync(id, request, ct);
        return OkResponse<object?>(null, "Payment recorded.");
    }
}
