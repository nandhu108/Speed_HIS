using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/indicators")]
public class IndicatorsController(QualityIndicatorService indicators) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityIndicatorView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndicatorDto>>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await indicators.GetAllAsync(search, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityIndicatorManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveIndicatorRequest request, CancellationToken ct)
    {
        var id = await indicators.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/indicators/{id}", id, "Indicator created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.QualityIndicatorManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveIndicatorRequest request, CancellationToken ct)
    {
        await indicators.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Indicator updated.");
    }

    [HttpGet("{id:long}/entries")]
    [HasPermission(PermissionRegistry.QualityIndicatorView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IndicatorEntryDto>>>> GetEntries(long id, CancellationToken ct) =>
        OkResponse(await indicators.GetEntriesAsync(id, ct));

    [HttpPost("{id:long}/entries")]
    [HasPermission(PermissionRegistry.QualityIndicatorEntryRecord)]
    public async Task<ActionResult<ApiResponse<long>>> RecordEntry(long id, RecordIndicatorEntryRequest request, CancellationToken ct)
    {
        var entryId = await indicators.RecordEntryAsync(id, request, ct);
        return CreatedResponse($"/api/v1/quality/indicators/{id}/entries/{entryId}", entryId, "Entry recorded.");
    }

    [HttpPost("{id:long}/entries/auto-compute")]
    [HasPermission(PermissionRegistry.QualityIndicatorEntryRecord)]
    public async Task<ActionResult<ApiResponse<long>>> AutoCompute(long id, [FromQuery] DateOnly period, CancellationToken ct)
    {
        var entryId = await indicators.AutoComputeAsync(id, period, ct);
        return OkResponse(entryId, "Entry auto-computed.");
    }
}
