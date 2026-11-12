using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/receivables")]
public class ReceivablesController(ReceivableService receivables) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.FinanceReceivableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReceivableDto>>>> GetAll([FromQuery] ReceivableQuery query, CancellationToken ct) =>
        OkResponse(await receivables.GetAllAsync(query, ct));

    [HttpGet("pending-bills")]
    [HasPermission(PermissionRegistry.FinanceReceivableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingCreditBillDto>>>> GetPendingBills(CancellationToken ct) =>
        OkResponse(await receivables.GetPendingBillsAsync(ct));

    [HttpGet("aging")]
    [HasPermission(PermissionRegistry.FinanceReceivableView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgingRowDto>>>> GetAging([FromQuery] DateOnly asOf, CancellationToken ct) =>
        OkResponse(await receivables.GetAgingAsync(asOf, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.FinanceReceivableManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateReceivableRequest request, CancellationToken ct)
    {
        var id = await receivables.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/finance/receivables/{id}", id, "Receivable raised.");
    }

    [HttpPost("{id:long}/collect")]
    [HasPermission(PermissionRegistry.FinanceReceivableManage)]
    public async Task<ActionResult<ApiResponse>> Collect(long id, RecordReceivableCollectionRequest request, CancellationToken ct)
    {
        await receivables.RecordCollectionAsync(id, request, ct);
        return OkResponse<object?>(null, "Collection recorded.");
    }
}
