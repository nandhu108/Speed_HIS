using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/credit-notes")]
public class CreditNotesController(BillingService billing) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.CreditNoteView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CreditNoteDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await billing.GetPendingCreditNotesAsync(ct));

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.CreditNoteApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecideCreditNoteRequest request, CancellationToken ct)
    {
        await billing.DecideCreditNoteAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Credit note approved." : "Credit note rejected.");
    }
}
