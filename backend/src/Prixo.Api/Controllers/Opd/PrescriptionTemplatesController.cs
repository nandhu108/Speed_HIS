using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Opd;

namespace Prixo.Api.Controllers.Opd;

[Route("api/v1/prescription-templates")]
public class PrescriptionTemplatesController(PrescriptionTemplateService templates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.TemplateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrescriptionTemplateDto>>>> GetByDoctor(
        [FromQuery] long doctorId, CancellationToken ct) =>
        OkResponse(await templates.GetByDoctorIdAsync(doctorId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.TemplateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SavePrescriptionTemplateRequest request, CancellationToken ct)
    {
        var id = await templates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/prescription-templates/{id}", id, "Template created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.TemplateManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SavePrescriptionTemplateRequest request, CancellationToken ct)
    {
        await templates.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Template updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.TemplateManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await templates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Template deleted.");
    }
}
