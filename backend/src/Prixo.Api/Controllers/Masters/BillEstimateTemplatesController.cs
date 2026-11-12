using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Billing;

namespace Prixo.Api.Controllers.Masters;

public class BillEstimateTemplatesController(BillEstimateTemplateService templates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BillEstimateTemplateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BillEstimateTemplateListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await templates.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BillEstimateTemplateView)]
    public async Task<ActionResult<ApiResponse<BillEstimateTemplateDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await templates.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BillEstimateTemplateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveBillEstimateTemplateRequest request, CancellationToken ct)
    {
        var id = await templates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/billestimatetemplates/{id}", id, "Bill estimate template created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.BillEstimateTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveBillEstimateTemplateRequest request, CancellationToken ct)
    {
        await templates.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Bill estimate template updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.BillEstimateTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await templates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Bill estimate template deleted.");
    }
}
