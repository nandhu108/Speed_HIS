using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab/collection")]
public class LabCollectionController(LabOrderService orders) : ApiControllerBase
{
    [HttpGet("worklist")]
    [HasPermission(PermissionRegistry.LabSampleCollect)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CollectionWorklistItemDto>>>> GetWorklist(CancellationToken ct) =>
        OkResponse(await orders.GetCollectionWorklistAsync(ct));

    [HttpGet("samples/awaiting-receipt")]
    [HasPermission(PermissionRegistry.LabSampleReceive)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SampleDto>>>> GetAwaitingReceipt(CancellationToken ct) =>
        OkResponse(await orders.GetSamplesAwaitingReceiptAsync(ct));

    [HttpPost("samples")]
    [HasPermission(PermissionRegistry.LabSampleCollect)]
    public async Task<ActionResult<ApiResponse<SampleDto>>> CollectSample(CollectSampleRequest request, CancellationToken ct)
    {
        var sample = await orders.CollectSampleAsync(request, ct);
        return CreatedResponse($"/api/v1/lab/collection/samples/{sample.Id}", sample, $"Sample {sample.SampleNumber} collected.");
    }

    [HttpPost("samples/{sampleId:long}/receive")]
    [HasPermission(PermissionRegistry.LabSampleReceive)]
    public async Task<ActionResult<ApiResponse<SampleDto>>> ReceiveSample(long sampleId, CancellationToken ct) =>
        OkResponse(await orders.ReceiveSampleAsync(sampleId, ct), "Sample received.");

    [HttpPost("samples/{sampleId:long}/reject")]
    [HasPermission(PermissionRegistry.LabSampleReject)]
    public async Task<ActionResult<ApiResponse>> RejectSample(long sampleId, RejectSampleRequest request, CancellationToken ct)
    {
        await orders.RejectSampleAsync(sampleId, request, ct);
        return OkResponse<object?>(null, "Sample rejected — awaiting recollection.");
    }
}
