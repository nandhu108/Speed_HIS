using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Er;

namespace Prixo.Api.Controllers.Er;

[Route("api/v1/emergency/visits/{visitId:long}")]
public class EmergencyAssessmentController(EmergencyAssessmentService assessments) : ApiControllerBase
{
    [HttpGet("assessment")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<AssessmentDto>>> GetAssessment(long visitId, CancellationToken ct) =>
        OkResponse(await assessments.GetAssessmentAsync(visitId, ct));

    [HttpPut("assessment")]
    [HasPermission(PermissionRegistry.EmergencyAssessmentManage)]
    public async Task<ActionResult<ApiResponse<AssessmentDto>>> SaveAssessment(long visitId, SaveAssessmentRequest request, CancellationToken ct) =>
        OkResponse(await assessments.SaveAssessmentAsync(visitId, request, ct), "Assessment saved.");

    [HttpGet("observations")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ObservationAllocationDto>>>> GetObservations(long visitId, CancellationToken ct) =>
        OkResponse(await assessments.GetObservationsAsync(visitId, ct));

    [HttpPost("observations")]
    [HasPermission(PermissionRegistry.EmergencyObservationManage)]
    public async Task<ActionResult<ApiResponse<long>>> AllocateObservation(long visitId, AllocateObservationRequest request, CancellationToken ct)
    {
        var id = await assessments.AllocateObservationAsync(visitId, request, ct);
        return CreatedResponse($"/api/v1/emergency/visits/{visitId}/observations/{id}", id, "Observation bed allocated.");
    }

    [HttpPost("observations/{allocationId:long}/release")]
    [HasPermission(PermissionRegistry.EmergencyObservationManage)]
    public async Task<ActionResult<ApiResponse>> ReleaseObservation(long visitId, long allocationId, CancellationToken ct)
    {
        await assessments.ReleaseObservationAsync(allocationId, ct);
        return OkResponse<object?>(null, "Observation bed released.");
    }
}
