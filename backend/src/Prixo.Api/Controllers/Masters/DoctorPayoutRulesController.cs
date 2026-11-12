using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Billing;

namespace Prixo.Api.Controllers.Masters;

public class DoctorPayoutRulesController(DoctorPayoutRuleService rules) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DoctorPayoutRuleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorPayoutRuleDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await rules.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DoctorPayoutRuleManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveDoctorPayoutRuleRequest request, CancellationToken ct)
    {
        var id = await rules.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/doctorpayoutrules/{id}", id, "Doctor payout rule created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DoctorPayoutRuleManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveDoctorPayoutRuleRequest request, CancellationToken ct)
    {
        await rules.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Doctor payout rule updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DoctorPayoutRuleManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await rules.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Doctor payout rule deleted.");
    }
}
