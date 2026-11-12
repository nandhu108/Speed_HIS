using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ot;

namespace Prixo.Api.Controllers.Ot;

[Route("api/v1/ot/bookings/{bookingId:long}")]
public class OtClinicalController(OtClinicalService clinical) : ApiControllerBase
{
    [HttpGet("checklist")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<ChecklistDto>>> GetChecklist(long bookingId, CancellationToken ct) =>
        OkResponse(await clinical.GetChecklistAsync(bookingId, ct));

    [HttpPut("checklist/sign-in")]
    [HasPermission(PermissionRegistry.OtChecklistManage)]
    public async Task<ActionResult<ApiResponse<ChecklistDto>>> UpdateSignIn(long bookingId, UpdateSignInRequest request, CancellationToken ct) =>
        OkResponse(await clinical.UpdateSignInAsync(bookingId, request, ct), "Sign In updated.");

    [HttpPut("checklist/time-out")]
    [HasPermission(PermissionRegistry.OtChecklistManage)]
    public async Task<ActionResult<ApiResponse<ChecklistDto>>> UpdateTimeOut(long bookingId, UpdateTimeOutRequest request, CancellationToken ct) =>
        OkResponse(await clinical.UpdateTimeOutAsync(bookingId, request, ct), "Time Out updated.");

    [HttpPut("checklist/sign-out")]
    [HasPermission(PermissionRegistry.OtChecklistManage)]
    public async Task<ActionResult<ApiResponse<ChecklistDto>>> UpdateSignOut(long bookingId, UpdateSignOutRequest request, CancellationToken ct) =>
        OkResponse(await clinical.UpdateSignOutAsync(bookingId, request, ct), "Sign Out updated.");

    [HttpGet("pre-anaesthesia")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<PreAnaesthesiaDto>>> GetPreAnaesthesia(long bookingId, CancellationToken ct) =>
        OkResponse(await clinical.GetPreAnaesthesiaAsync(bookingId, ct));

    [HttpPut("pre-anaesthesia")]
    [HasPermission(PermissionRegistry.OtAnaesthesiaNoteManage)]
    public async Task<ActionResult<ApiResponse<PreAnaesthesiaDto>>> SavePreAnaesthesia(long bookingId, SavePreAnaesthesiaRequest request, CancellationToken ct) =>
        OkResponse(await clinical.SavePreAnaesthesiaAsync(bookingId, request, ct), "Pre-anaesthesia assessment saved.");

    [HttpGet("intra-op")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<IntraOpDto>>> GetIntraOp(long bookingId, CancellationToken ct) =>
        OkResponse(await clinical.GetIntraOpAsync(bookingId, ct));

    [HttpPut("intra-op")]
    [HasPermission(PermissionRegistry.OtAnaesthesiaNoteManage)]
    public async Task<ActionResult<ApiResponse<IntraOpDto>>> SaveIntraOp(long bookingId, SaveIntraOpRequest request, CancellationToken ct) =>
        OkResponse(await clinical.SaveIntraOpAsync(bookingId, request, ct), "Intra-op anaesthesia record saved.");

    [HttpGet("operative-note")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<OperativeNoteDto>>> GetOperativeNote(long bookingId, CancellationToken ct) =>
        OkResponse(await clinical.GetOperativeNoteAsync(bookingId, ct));

    [HttpPut("operative-note")]
    [HasPermission(PermissionRegistry.OtOperativeNoteManage)]
    public async Task<ActionResult<ApiResponse<OperativeNoteDto>>> SaveOperativeNote(long bookingId, SaveOperativeNoteRequest request, CancellationToken ct) =>
        OkResponse(await clinical.SaveOperativeNoteAsync(bookingId, request, ct), "Operative note saved.");

    [HttpGet("post-op-note")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<PostOpNoteDto>>> GetPostOpNote(long bookingId, CancellationToken ct) =>
        OkResponse(await clinical.GetPostOpNoteAsync(bookingId, ct));

    [HttpPut("post-op-note")]
    [HasPermission(PermissionRegistry.OtOperativeNoteManage)]
    public async Task<ActionResult<ApiResponse<PostOpNoteDto>>> SavePostOpNote(long bookingId, SavePostOpNoteRequest request, CancellationToken ct) =>
        OkResponse(await clinical.SavePostOpNoteAsync(bookingId, request, ct), "Post-op note saved.");
}
