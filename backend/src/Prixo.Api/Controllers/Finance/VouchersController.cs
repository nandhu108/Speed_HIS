using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/vouchers")]
public class VouchersController(VoucherService vouchers) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.FinanceVoucherView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VoucherDto>>>> GetAll([FromQuery] VoucherQuery query, CancellationToken ct) =>
        OkResponse(await vouchers.GetListAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.FinanceVoucherView)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await vouchers.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.FinanceVoucherCreate)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Post(PostVoucherRequest request, CancellationToken ct)
    {
        var voucher = await vouchers.PostAsync(request, ct);
        return CreatedResponse($"/api/v1/finance/vouchers/{voucher.Id}", voucher, "Voucher posted.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.FinanceVoucherCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelVoucherRequest request, CancellationToken ct)
    {
        await vouchers.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Voucher cancelled.");
    }
}
