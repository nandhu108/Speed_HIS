using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Icd;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Masters;

[Route("api/v1/icd-codes")]
public class IcdCodesController(IcdService icd) : ApiControllerBase
{
    private const long MaxImportBytes = 10 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.IcdView)]
    public async Task<ActionResult<ApiResponse<PagedResult<IcdCodeDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await icd.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IcdManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveIcdCodeRequest request, CancellationToken ct)
    {
        var id = await icd.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/icd-codes/{id}", id, "ICD code created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.IcdManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveIcdCodeRequest request, CancellationToken ct)
    {
        await icd.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "ICD code updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.IcdManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await icd.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "ICD code deleted.");
    }

    /// <summary>CSV upsert: code,description[,category] with optional header row.</summary>
    [HttpPost("import")]
    [HasPermission(PermissionRegistry.IcdImport)]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<ActionResult<ApiResponse<IcdImportResultDto>>> Import(
        IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var result = await icd.ImportCsvAsync(stream, ct);
        return OkResponse(result,
            $"Import complete: {result.Inserted} inserted, {result.Updated} updated, {result.Skipped} skipped.");
    }
}
