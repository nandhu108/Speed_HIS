using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Emr;

namespace Prixo.Api.Controllers.Emr;

[Route("api/v1/emr/patients/{patientId:long}")]
public class HistoriesController(HistoryService histories) : ApiControllerBase
{
    [HttpGet("medical-history")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicalHistoryDto>>>> GetMedical(long patientId, CancellationToken ct) =>
        OkResponse(await histories.GetMedicalHistoryAsync(patientId, ct));

    [HttpPost("medical-history")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddMedical(long patientId, AddMedicalHistoryRequest request, CancellationToken ct)
    {
        var id = await histories.AddMedicalHistoryAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/medical-history/{id}", id, "Medical history recorded.");
    }

    [HttpGet("surgical-history")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SurgicalHistoryDto>>>> GetSurgical(long patientId, CancellationToken ct) =>
        OkResponse(await histories.GetSurgicalHistoryAsync(patientId, ct));

    [HttpPost("surgical-history")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddSurgical(long patientId, AddSurgicalHistoryRequest request, CancellationToken ct)
    {
        var id = await histories.AddSurgicalHistoryAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/surgical-history/{id}", id, "Surgical history recorded.");
    }

    [HttpGet("family-history")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FamilyHistoryDto>>>> GetFamily(long patientId, CancellationToken ct) =>
        OkResponse(await histories.GetFamilyHistoryAsync(patientId, ct));

    [HttpPost("family-history")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddFamily(long patientId, AddFamilyHistoryRequest request, CancellationToken ct)
    {
        var id = await histories.AddFamilyHistoryAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/family-history/{id}", id, "Family history recorded.");
    }

    [HttpGet("immunizations")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ImmunizationDto>>>> GetImmunizations(long patientId, CancellationToken ct) =>
        OkResponse(await histories.GetImmunizationsAsync(patientId, ct));

    [HttpPost("immunizations")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddImmunization(long patientId, AddImmunizationRequest request, CancellationToken ct)
    {
        var id = await histories.AddImmunizationAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/immunizations/{id}", id, "Immunization recorded.");
    }

    [HttpGet("allergies")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AllergyDto>>>> GetAllergies(
        long patientId, [FromQuery] bool activeOnly, CancellationToken ct) =>
        OkResponse(await histories.GetAllergiesAsync(patientId, activeOnly, ct));

    [HttpPost("allergies")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddAllergy(long patientId, AddAllergyRequest request, CancellationToken ct)
    {
        var id = await histories.AddAllergyAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/allergies/{id}", id, "Allergy recorded.");
    }

    [HttpPost("allergies/{allergyId:long}/deactivate")]
    [HasPermission(PermissionRegistry.EmrHistoryManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateAllergy(long patientId, long allergyId, CancellationToken ct)
    {
        await histories.DeactivateAllergyAsync(patientId, allergyId, ct);
        return OkResponse<object?>(null, "Allergy deactivated.");
    }

    /// <summary>The "prescription warning" hook (plan §11) — advisory cross-check, not a hard block.</summary>
    [HttpGet("allergy-check")]
    [HasPermission(PermissionRegistry.EmrHistoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AllergyMatchDto>>>> CheckDrugAllergies(
        long patientId, [FromQuery] List<string> drugNames, CancellationToken ct) =>
        OkResponse(await histories.CheckDrugAllergiesAsync(patientId, drugNames, ct));
}
