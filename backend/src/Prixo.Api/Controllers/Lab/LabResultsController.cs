using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab")]
public class LabResultsController(LabOrderService orders) : ApiControllerBase
{
    [HttpGet("worklist")]
    [HasPermission(PermissionRegistry.LabResultCreate)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentWorklistItemDto>>>> GetDepartmentWorklist(
        [FromQuery] long? departmentId, CancellationToken ct) =>
        OkResponse(await orders.GetDepartmentWorklistAsync(departmentId, ct));

    [HttpGet("results/queue")]
    [HasPermission(PermissionRegistry.LabResultVerify)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentWorklistItemDto>>>> GetResultQueue(
        [FromQuery] LabOrderItemStatus status, CancellationToken ct) =>
        OkResponse(await orders.GetResultQueueAsync(status, ct));

    [HttpGet("order-items/{itemId:long}/result")]
    [HasPermission(PermissionRegistry.LabOrderView)]
    public async Task<ActionResult<ApiResponse<LabResultDto>>> GetResult(long itemId, CancellationToken ct) =>
        OkResponse(await orders.GetResultByItemIdAsync(itemId, ct));

    [HttpPost("order-items/{itemId:long}/result")]
    [HasPermission(PermissionRegistry.LabResultCreate)]
    public async Task<ActionResult<ApiResponse<LabResultDto>>> EnterResult(
        long itemId, EnterResultRequest request, CancellationToken ct) =>
        OkResponse(await orders.EnterResultAsync(itemId, request, ct), "Result saved.");

    [HttpPost("order-items/{itemId:long}/result/verify")]
    [HasPermission(PermissionRegistry.LabResultVerify)]
    public async Task<ActionResult<ApiResponse<LabResultDto>>> VerifyResult(long itemId, CancellationToken ct) =>
        OkResponse(await orders.VerifyResultAsync(itemId, ct), "Result verified.");

    [HttpPost("order-items/{itemId:long}/result/approve")]
    [HasPermission(PermissionRegistry.LabResultApprove)]
    public async Task<ActionResult<ApiResponse<LabResultDto>>> ApproveResult(long itemId, CancellationToken ct) =>
        OkResponse(await orders.ApproveResultAsync(itemId, ct), "Result approved.");
}
