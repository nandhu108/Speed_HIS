using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/files")]
public class RecordFilesController(RecordFileService files) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdFileView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RecordFileListItemDto>>>> GetAll([FromQuery] RecordFileQuery query, CancellationToken ct) =>
        OkResponse(await files.GetListAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.MrdFileView)]
    public async Task<ActionResult<ApiResponse<RecordFileDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await files.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MrdFileCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateRecordFileRequest request, CancellationToken ct)
    {
        var id = await files.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/mrd/files/{id}", id, "Medical record file created.");
    }

    [HttpGet("{id:long}/movements")]
    [HasPermission(PermissionRegistry.MrdFileView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FileMovementDto>>>> GetMovements(long id, CancellationToken ct) =>
        OkResponse(await files.GetMovementsAsync(id, ct));

    [HttpPost("{id:long}/movements")]
    [HasPermission(PermissionRegistry.MrdFileManage)]
    public async Task<ActionResult<ApiResponse<long>>> RecordMovement(long id, RecordMovementRequest request, CancellationToken ct) =>
        OkResponse(await files.RecordMovementAsync(id, request, ct), "Movement recorded.");
}
