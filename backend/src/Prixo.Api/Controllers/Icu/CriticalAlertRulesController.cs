using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Icu;

namespace Prixo.Api.Controllers.Icu;

[Route("api/v1/icu/alert-rules")]
public class CriticalAlertRulesController(CriticalAlertRuleService rules) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IcuRuleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CriticalAlertRuleDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await rules.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IcuRuleManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveCriticalAlertRuleRequest request, CancellationToken ct)
    {
        var id = await rules.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/icu/alert-rules/{id}", id, "Critical alert rule created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.IcuRuleManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveCriticalAlertRuleRequest request, CancellationToken ct)
    {
        await rules.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Critical alert rule updated.");
    }
}
