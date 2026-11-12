using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/admissions")]
public class AdmissionsController(AdmissionService admissions) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AdmissionView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdmissionListItemDto>>>> GetPaged(
        [FromQuery] AdmissionQuery query, CancellationToken ct) =>
        OkResponse(await admissions.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.AdmissionView)]
    public async Task<ActionResult<ApiResponse<AdmissionDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await admissions.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.AdmissionCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Admit(AdmitPatientRequest request, CancellationToken ct)
    {
        var id = await admissions.AdmitAsync(request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{id}", id, "Patient admitted.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.AdmissionCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelAdmissionRequest request, CancellationToken ct)
    {
        await admissions.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Admission cancelled.");
    }

    [HttpPost("{id:long}/transfer-bed")]
    [HasPermission(PermissionRegistry.BedManage)]
    public async Task<ActionResult<ApiResponse>> TransferBed(long id, TransferBedRequest request, CancellationToken ct)
    {
        await admissions.TransferBedAsync(id, request, ct);
        return OkResponse<object?>(null, "Bed transferred.");
    }

    [HttpPost("{id:long}/transfer-consultant")]
    [HasPermission(PermissionRegistry.ConsultantManage)]
    public async Task<ActionResult<ApiResponse>> TransferConsultant(long id, TransferConsultantRequest request, CancellationToken ct)
    {
        await admissions.TransferConsultantAsync(id, request, ct);
        return OkResponse<object?>(null, "Primary consultant transferred.");
    }

    [HttpPost("{id:long}/consultants")]
    [HasPermission(PermissionRegistry.ConsultantManage)]
    public async Task<ActionResult<ApiResponse>> AddSecondaryConsultant(long id, AddSecondaryConsultantRequest request, CancellationToken ct)
    {
        await admissions.AddSecondaryConsultantAsync(id, request, ct);
        return OkResponse<object?>(null, "Secondary consultant added.");
    }

    [HttpPost("{id:long}/consultants/{consultantId:long}/end")]
    [HasPermission(PermissionRegistry.ConsultantManage)]
    public async Task<ActionResult<ApiResponse>> EndSecondaryConsultant(long id, long consultantId, CancellationToken ct)
    {
        await admissions.EndSecondaryConsultantAsync(id, consultantId, ct);
        return OkResponse<object?>(null, "Consultant mapping ended.");
    }

    [HttpPost("{id:long}/credit-limit")]
    [HasPermission(PermissionRegistry.AdmissionCreate)]
    public async Task<ActionResult<ApiResponse>> SetCreditLimit(long id, SetCreditLimitRequest request, CancellationToken ct)
    {
        await admissions.SetCreditLimitAsync(id, request, ct);
        return OkResponse<object?>(null, "Credit limit updated.");
    }

    [HttpGet("{id:long}/admission-slip")]
    [HasPermission(PermissionRegistry.AdmissionView)]
    public async Task<IActionResult> GetAdmissionSlip(long id, CancellationToken ct)
    {
        var pdf = await admissions.GenerateAdmissionSlipAsync(id, ct);
        return File(pdf, "application/pdf", $"admission-slip-{id}.pdf");
    }
}
