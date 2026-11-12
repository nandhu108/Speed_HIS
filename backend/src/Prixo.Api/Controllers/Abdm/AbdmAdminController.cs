using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients.Abdm;

namespace Prixo.Api.Controllers.Abdm;

/// <summary>
/// ABDM Administration & Governance controller.
/// Allows hospital administrators to enable/disable ABDM and individual milestones (M1, M2, M3),
/// configure facility credentials, and monitor ecosystem health and transaction statistics.
/// </summary>
[Route("api/v1/abdm/admin")]
public class AbdmAdminController(AbdmAdminService admin) : ApiControllerBase
{
    /// <summary>Get ABDM status, milestone toggles, and ecosystem statistics.</summary>
    [HttpGet("status")]
    [HasPermission(PermissionRegistry.AbdmAdminManage)]
    public async Task<ActionResult<ApiResponse<AbdmAdminStatusDto>>> GetStatus(CancellationToken ct) =>
        OkResponse(await admin.GetStatusAsync(ct));

    /// <summary>Update ABDM configuration, enable/disable switches, and gateway credentials.</summary>
    [HttpPost("config")]
    [HasPermission(PermissionRegistry.AbdmAdminManage)]
    public async Task<ActionResult<ApiResponse<AbdmAdminStatusDto>>> UpdateConfig(
        [FromBody] UpdateAbdmAdminConfigRequest request, CancellationToken ct) =>
        OkResponse(await admin.UpdateConfigAsync(request, ct), "ABDM configuration updated.");

    /// <summary>Quick toggle for ABDM Master switch or individual milestones (m1, m2, m3).</summary>
    [HttpPost("toggle")]
    [HasPermission(PermissionRegistry.AbdmAdminManage)]
    public async Task<ActionResult<ApiResponse<AbdmAdminStatusDto>>> ToggleMilestone(
        [FromBody] ToggleMilestoneRequest request, CancellationToken ct) =>
        OkResponse(await admin.ToggleMilestoneAsync(request, ct), "Milestone toggle saved.");

    /// <summary>Test gateway connectivity and sandbox health probe.</summary>
    [HttpGet("test-connection")]
    [HasPermission(PermissionRegistry.AbdmAdminManage)]
    public async Task<ActionResult<ApiResponse<object>>> TestConnection(CancellationToken ct) =>
        OkResponse(await admin.TestConnectionAsync(ct));
}
