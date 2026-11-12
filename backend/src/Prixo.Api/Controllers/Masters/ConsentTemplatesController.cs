using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Consents;

namespace Prixo.Api.Controllers.Masters;

[Route("api/v1/consent-templates")]
public class ConsentTemplatesController(ConsentTemplateService templates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ConsentTemplateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsentTemplateDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await templates.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.ConsentTemplateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveConsentTemplateRequest request, CancellationToken ct)
    {
        var id = await templates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/consent-templates/{id}", id, "Consent template created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ConsentTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveConsentTemplateRequest request, CancellationToken ct)
    {
        await templates.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Consent template updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ConsentTemplateManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await templates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Consent template deleted.");
    }
}
