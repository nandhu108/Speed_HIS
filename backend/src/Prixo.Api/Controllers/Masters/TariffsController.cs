using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Tariffs;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Masters;

public class TariffsController(TariffService tariffs, IReportExportService excel, IPdfService pdf) : ApiControllerBase
{
    private const long MaxImportBytes = 10 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.TariffView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TariffListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await tariffs.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.TariffView)]
    public async Task<ActionResult<ApiResponse<TariffDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await tariffs.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.TariffManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveTariffRequest request, CancellationToken ct)
    {
        var id = await tariffs.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/tariffs/{id}", id, "Tariff created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.TariffManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveTariffRequest request, CancellationToken ct)
    {
        await tariffs.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Tariff updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.TariffManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await tariffs.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Tariff deleted.");
    }

    /// <summary>CSV/Excel export of every tariff item, in the exact column shape Import expects back.</summary>
    [HttpGet("export")]
    [HasPermission(PermissionRegistry.TariffView)]
    public async Task<IActionResult> Export([FromQuery] string format, CancellationToken ct)
    {
        var result = await tariffs.GetExportAsync(ct);
        return ReportExportHelper.ToFileResult(result, format, "tariff-list", null, excel, pdf);
    }

    /// <summary>CSV upsert: TariffCode,TariffName,PatientCategoryCode,EffectiveFrom,EffectiveTo,ServiceCode,RoomTypeCode,Rate — optional header row.</summary>
    [HttpPost("import")]
    [HasPermission(PermissionRegistry.TariffImport)]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<ActionResult<ApiResponse<TariffImportResultDto>>> Import(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var result = await tariffs.ImportCsvAsync(stream, ct);
        return OkResponse(result,
            $"Import complete: {result.TariffsInserted} tariffs created, {result.TariffsUpdated} updated, {result.ItemsUpserted} rate rows applied.");
    }
}
