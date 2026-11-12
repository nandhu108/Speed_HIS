using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/calibrations")]
public class CalibrationsController(CalibrationService calibrations) : ApiControllerBase
{
    private const long MaxCertificateRequestBytes = 11 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedCalibrationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CalibrationDto>>>> GetAll([FromQuery] CalibrationQuery query, CancellationToken ct) =>
        OkResponse(await calibrations.GetAllAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedCalibrationManage)]
    [RequestSizeLimit(MaxCertificateRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        [FromForm] long equipmentId, [FromForm] DateOnly calibrationDate, [FromForm] DateOnly nextDueDate,
        [FromForm] string? performedBy, [FromForm] string? certificateNumber, [FromForm] string? notes, IFormFile? certificate, CancellationToken ct)
    {
        var request = new SaveCalibrationRequest(equipmentId, calibrationDate, nextDueDate, performedBy, certificateNumber, notes);
        Stream? stream = certificate is { Length: > 0 } ? certificate.OpenReadStream() : null;
        try
        {
            var id = await calibrations.CreateAsync(request, stream, certificate?.FileName, ct);
            return CreatedResponse($"/api/v1/biomed/calibrations/{id}", id, "Calibration recorded.");
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }

    [HttpGet("{id:long}/certificate")]
    [HasPermission(PermissionRegistry.BiomedCalibrationView)]
    public async Task<IActionResult> DownloadCertificate(long id, CancellationToken ct)
    {
        var (content, fileName) = await calibrations.OpenCertificateAsync(id, ct);
        return File(content, "application/octet-stream", fileName);
    }
}
