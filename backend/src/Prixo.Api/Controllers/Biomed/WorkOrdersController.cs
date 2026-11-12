using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/work-orders")]
public class WorkOrdersController(WorkOrderService workOrders) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedWorkOrderView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkOrderDto>>>> GetAll([FromQuery] WorkOrderQuery query, CancellationToken ct) =>
        OkResponse(await workOrders.GetListAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderView)]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await workOrders.GetByIdAsync(id, ct));

    [HttpPost("from-complaint")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateFromComplaint(CreateWorkOrderFromComplaintRequest request, CancellationToken ct)
    {
        var id = await workOrders.CreateFromComplaintAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/work-orders/{id}", id, "Work order created.");
    }

    [HttpPost("{id:long}/assign")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderManage)]
    public async Task<ActionResult<ApiResponse>> Assign(long id, AssignWorkOrderRequest request, CancellationToken ct)
    {
        await workOrders.AssignAsync(id, request, ct);
        return OkResponse<object?>(null, "Work order assigned.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderManage)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CompleteWorkOrderRequest request, CancellationToken ct)
    {
        await workOrders.CompleteAsync(id, request, ct);
        return OkResponse<object?>(null, "Work order completed.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelWorkOrderRequest request, CancellationToken ct)
    {
        await workOrders.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Work order cancelled.");
    }

    [HttpPost("{id:long}/spare-usage")]
    [HasPermission(PermissionRegistry.BiomedWorkOrderManage)]
    public async Task<ActionResult<ApiResponse<long>>> RecordSpareUsage(long id, RecordSpareUsageRequest request, CancellationToken ct) =>
        OkResponse(await workOrders.RecordSpareUsageAsync(id, request, ct), "Spare usage recorded.");
}
