using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.DoctorPortal;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Lab;
using Prixo.Application.Features.Opd;

namespace Prixo.Api.Controllers.DoctorPortal;

/// <summary>Doctor Portal (plan §22) — same staff JWT/RBAC as every other module; see DoctorPortalService's doc comment for why this is a thin "my own" wrapper rather than a parallel engine.</summary>
[Route("api/v1/doctor-portal")]
public class DoctorPortalController(DoctorPortalService doctorPortal) : ApiControllerBase
{
    [HttpGet("dashboard")]
    [HasPermission(PermissionRegistry.DoctorPortalDashboardView)]
    public async Task<ActionResult<ApiResponse<DoctorDashboardDto>>> GetDashboard(CancellationToken ct) =>
        OkResponse(await doctorPortal.GetDashboardAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct));

    [HttpGet("queue")]
    [HasPermission(PermissionRegistry.DoctorPortalQueueView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QueueEntryDto>>>> GetMyQueue([FromQuery] DateOnly? date, CancellationToken ct) =>
        OkResponse(await doctorPortal.GetMyQueueAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));

    [HttpGet("inpatients")]
    [HasPermission(PermissionRegistry.DoctorPortalIpRoundsView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdmissionListItemDto>>>> GetMyInpatients(CancellationToken ct) =>
        OkResponse(await doctorPortal.GetMyInpatientsAsync(ct));

    [HttpGet("orders")]
    [HasPermission(PermissionRegistry.DoctorPortalOrdersView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LabOrderListItemDto>>>> GetMyOrders(CancellationToken ct) =>
        OkResponse(await doctorPortal.GetMyOrdersAsync(ct));

    [HttpGet("discharge-approvals")]
    [HasPermission(PermissionRegistry.DoctorPortalDischargeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingDischargeSummaryDto>>>> GetMyPendingDischargeSummaries(CancellationToken ct) =>
        OkResponse(await doctorPortal.GetMyPendingDischargeSummariesAsync(ct));

    [HttpPost("discharge-approvals/{admissionId:long}/approve")]
    [HasPermission(PermissionRegistry.DischargeApproveSummary)]
    public async Task<ActionResult<ApiResponse>> ApproveMyDischargeSummary(long admissionId, CancellationToken ct)
    {
        await doctorPortal.ApproveMyDischargeSummaryAsync(admissionId, ct);
        return OkResponse<object?>(null, "Discharge summary approved.");
    }

    [HttpGet("opinion-requests")]
    [HasPermission(PermissionRegistry.DoctorPortalOpinionView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpinionRequestDto>>>> GetMyOpinionRequests(CancellationToken ct) =>
        OkResponse(await doctorPortal.GetMyOpinionRequestsAsync(ct));

    [HttpPut("opinion-requests/{referralId:long}")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveMyOpinion(long referralId, SaveReferralOpinionRequest request, CancellationToken ct)
    {
        await doctorPortal.SaveMyOpinionAsync(referralId, request, ct);
        return OkResponse<object?>(null, "Opinion recorded.");
    }
}
