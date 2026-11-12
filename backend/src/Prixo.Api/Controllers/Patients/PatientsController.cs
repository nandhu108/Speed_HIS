using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Patients;

public class PatientsController(PatientService patients) : ApiControllerBase
{
    private const long MaxPhotoRequestBytes = 6 * 1024 * 1024;
    private const long MaxDocumentRequestBytes = 16 * 1024 * 1024;

    [HttpGet]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientListItemDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await patients.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await patients.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PatientCreate)]
    public async Task<ActionResult<ApiResponse<RegisterPatientResultDto>>> Register(
        RegisterPatientRequest request, CancellationToken ct)
    {
        var result = await patients.RegisterAsync(request, ct);
        return CreatedResponse($"/api/v1/patients/{result.Id}", result, $"Patient registered with UHID {result.Uhid}.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PatientUpdate)]
    public async Task<ActionResult<ApiResponse>> Update(long id, UpdatePatientRequest request, CancellationToken ct)
    {
        await patients.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Patient updated.");
    }

    /// <summary>Pre-save duplicate check for the registration/edit wizard.</summary>
    [HttpPost("duplicates/check")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DuplicateCandidateDto>>>> CheckDuplicates(
        CheckDuplicatesRequest request, CancellationToken ct) =>
        OkResponse(await patients.CheckDuplicatesAsync(request, ct));

    [HttpGet("{id:long}/duplicates")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DuplicateCandidateDto>>>> GetDuplicates(
        long id, CancellationToken ct) =>
        OkResponse(await patients.GetDuplicatesForPatientAsync(id, ct));

    /// <summary>Merges {mergedPatientId} into {id} (the survivor) in a single transaction.</summary>
    [HttpPost("{id:long}/merge")]
    [HasPermission(PermissionRegistry.PatientMerge)]
    public async Task<ActionResult<ApiResponse>> Merge(long id, MergePatientRequest request, CancellationToken ct)
    {
        await patients.MergeAsync(id, request, ct);
        return OkResponse<object?>(null, "Patients merged.");
    }

    [HttpPost("{id:long}/block")]
    [HasPermission(PermissionRegistry.PatientBlock)]
    public async Task<ActionResult<ApiResponse>> Block(long id, BlockPatientRequest request, CancellationToken ct)
    {
        await patients.BlockAsync(id, request, ct);
        return OkResponse<object?>(null, "Patient blocked.");
    }

    [HttpPost("{id:long}/unblock")]
    [HasPermission(PermissionRegistry.PatientBlock)]
    public async Task<ActionResult<ApiResponse>> Unblock(long id, CancellationToken ct)
    {
        await patients.UnblockAsync(id, ct);
        return OkResponse<object?>(null, "Patient unblocked.");
    }

    [HttpPost("{id:long}/photo")]
    [HasPermission(PermissionRegistry.PatientUpdate)]
    [RequestSizeLimit(MaxPhotoRequestBytes)]
    public async Task<ActionResult<ApiResponse>> UploadPhoto(long id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded photo is empty.");
        }

        await using var stream = file.OpenReadStream();
        await patients.SavePhotoAsync(id, stream, file.FileName, file.ContentType, file.Length, ct);
        return OkResponse<object?>(null, "Photo uploaded.");
    }

    [HttpGet("{id:long}/photo")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<IActionResult> GetPhoto(long id, CancellationToken ct)
    {
        var (content, contentType) = await patients.OpenPhotoAsync(id, ct);
        return File(content, contentType);
    }

    [HttpPost("{id:long}/documents")]
    [HasPermission(PermissionRegistry.PatientUpdate)]
    [RequestSizeLimit(MaxDocumentRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> AddDocument(
        long id, [FromForm] AddPatientDocumentRequest request, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var documentId = await patients.AddDocumentAsync(
            id, request.DocumentType, stream, file.FileName, file.ContentType, file.Length, request.Description, ct);
        return CreatedResponse($"/api/v1/patients/{id}/documents/{documentId}", documentId, "Document uploaded.");
    }

    [HttpGet("{id:long}/documents")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientDocumentDto>>>> GetDocuments(
        long id, CancellationToken ct) =>
        OkResponse(await patients.GetDocumentsAsync(id, ct));

    [HttpGet("{id:long}/documents/{documentId:long}/download")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<IActionResult> DownloadDocument(long id, long documentId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await patients.OpenDocumentAsync(id, documentId, ct);
        return File(content, contentType, fileName);
    }

    [HttpGet("{id:long}/consents")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsentDto>>>> GetConsents(
        long id, CancellationToken ct) =>
        OkResponse(await patients.GetConsentsAsync(id, ct));

    [HttpPost("{id:long}/consents")]
    [HasPermission(PermissionRegistry.PatientUpdate)]
    [RequestSizeLimit(MaxDocumentRequestBytes)]
    public async Task<ActionResult<ApiResponse<long>>> CaptureConsent(
        long id, [FromForm] CaptureConsentRequest request, IFormFile? scan, CancellationToken ct)
    {
        Stream? scanStream = null;
        try
        {
            if (scan is { Length: > 0 })
            {
                scanStream = scan.OpenReadStream();
            }
            var consentId = await patients.CaptureConsentAsync(id, request, scanStream, scan?.FileName, ct);
            return CreatedResponse($"/api/v1/patients/{id}/consents/{consentId}", consentId, "Consent recorded.");
        }
        finally
        {
            if (scanStream is not null)
            {
                await scanStream.DisposeAsync();
            }
        }
    }

    /// <summary>Printable registration card (PDF) with the UHID QR code.</summary>
    [HttpGet("{id:long}/card")]
    [HasPermission(PermissionRegistry.PatientView)]
    public async Task<IActionResult> GetCard(long id, CancellationToken ct)
    {
        var pdf = await patients.GenerateRegistrationCardAsync(id, ct);
        return File(pdf, "application/pdf", $"registration-card-{id}.pdf");
    }
}
