using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Masters;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Common;
using Prixo.Application.Features.Quality;
using Prixo.Domain.Entities.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/committees")]
public class CommitteesController(QualityCommitteeService service)
    : SimpleMasterControllerBase<QualityCommittee>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityCommitteeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "quality/committees", "Committee created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Committee updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Committee deleted.", ct);
}

[Route("api/v1/quality/committee-meetings")]
public class CommitteeMeetingsController(QualityCommitteeMeetingService meetings) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityCommitteeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommitteeMeetingDto>>>> GetAll(
        [FromQuery] long? committeeId, CancellationToken ct) =>
        OkResponse(await meetings.GetAllAsync(committeeId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveCommitteeMeetingRequest request, CancellationToken ct)
    {
        var id = await meetings.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/committee-meetings/{id}", id, "Meeting recorded.");
    }

    [HttpPost("{meetingId:long}/action-items/{actionItemId:long}/complete")]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public async Task<ActionResult<ApiResponse>> CompleteActionItem(long meetingId, long actionItemId, CancellationToken ct)
    {
        await meetings.CompleteActionItemAsync(meetingId, actionItemId, ct);
        return OkResponse<object?>(null, "Action item completed.");
    }
}

[Route("api/v1/quality/nabh-standards")]
public class NabhStandardsController(QualityNabhStandardService service)
    : SimpleMasterControllerBase<QualityNabhStandard>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityCommitteeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "quality/nabh-standards", "NABH standard created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "NABH standard updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.QualityCommitteeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "NABH standard deleted.", ct);
}
