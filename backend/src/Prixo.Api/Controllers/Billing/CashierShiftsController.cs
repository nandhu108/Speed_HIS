using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/cashier-shifts")]
public class CashierShiftsController(CashierShiftService shifts) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.CashierShiftView)]
    public async Task<ActionResult<ApiResponse<PagedResult<CashierShiftDto>>>> GetPaged(
        [FromQuery] CashierShiftQuery query, CancellationToken ct) =>
        OkResponse(await shifts.GetPagedAsync(query, ct));

    [HttpGet("mine")]
    [HasPermission(PermissionRegistry.CashierShiftManage)]
    public async Task<ActionResult<ApiResponse<CashierShiftDto?>>> GetMine(CancellationToken ct) =>
        OkResponse(await shifts.GetMyOpenShiftAsync(ct));

    [HttpPost("open")]
    [HasPermission(PermissionRegistry.CashierShiftManage)]
    public async Task<ActionResult<ApiResponse<long>>> Open(OpenCashierShiftRequest request, CancellationToken ct)
    {
        var id = await shifts.OpenAsync(request, ct);
        return CreatedResponse($"/api/v1/billing/cashier-shifts/{id}", id, "Cashier shift opened.");
    }

    [HttpGet("{id:long}/collection")]
    [HasPermission(PermissionRegistry.CashierShiftManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DailyCollectionModeDto>>>> GetCollection(long id, CancellationToken ct) =>
        OkResponse(await shifts.GetCollectionAsync(id, ct));

    [HttpPost("{id:long}/close")]
    [HasPermission(PermissionRegistry.CashierShiftManage)]
    public async Task<ActionResult<ApiResponse<CashierShiftDto>>> Close(long id, CloseCashierShiftRequest request, CancellationToken ct)
    {
        var result = await shifts.CloseAsync(id, request, ct);
        return OkResponse(result, "Cashier shift closed.");
    }

    [HttpPost("{id:long}/receive")]
    [HasPermission(PermissionRegistry.CashierShiftReceive)]
    public async Task<ActionResult<ApiResponse>> Receive(long id, CancellationToken ct)
    {
        await shifts.ReceiveAsync(id, ct);
        return OkResponse<object?>(null, "Cash received.");
    }
}
