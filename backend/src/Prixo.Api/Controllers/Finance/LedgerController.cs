using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/ledger")]
public class LedgerController(LedgerService ledger) : ApiControllerBase
{
    [HttpGet("{accountId:long}")]
    [HasPermission(PermissionRegistry.FinanceLedgerView)]
    public async Task<ActionResult<ApiResponse<AccountLedgerDto>>> GetAccountLedger(
        long accountId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await ledger.GetAccountLedgerAsync(accountId, fromDate, toDate, ct));
}
