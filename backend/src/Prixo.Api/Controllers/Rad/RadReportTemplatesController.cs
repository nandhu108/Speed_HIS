using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad/templates")]
public class RadReportTemplatesController(RadReportTemplateService templates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RadTemplateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RadReportTemplateListItemDto>>>> GetAll(
        [FromQuery] long? radiologyTestId, CancellationToken ct) =>
        OkResponse(await templates.GetAllAsync(radiologyTestId, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.RadTemplateView)]
    public async Task<ActionResult<ApiResponse<RadReportTemplateDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await templates.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.RadTemplateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveRadReportTemplateRequest request, CancellationToken ct)
    {
        var id = await templates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/rad/templates/{id}", id, "Report template created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.RadTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveRadReportTemplateRequest request, CancellationToken ct)
    {
        await templates.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Report template updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.RadTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await templates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Report template deleted.");
    }
}
