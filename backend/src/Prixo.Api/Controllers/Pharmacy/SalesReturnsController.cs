using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/sales-returns")]
public class SalesReturnsController(PharmacySaleService sales) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SalesReturnView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesReturnListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await sales.GetSalesReturnsAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.SalesReturnCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateSalesReturnRequest request, CancellationToken ct)
    {
        var id = await sales.CreateSalesReturnAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/sales-returns/{id}", id, "Sales return posted.");
    }
}
