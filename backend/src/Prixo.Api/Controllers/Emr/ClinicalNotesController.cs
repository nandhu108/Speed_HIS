using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Emr;

namespace Prixo.Api.Controllers.Emr;

[Route("api/v1/emr/patients/{patientId:long}/clinical-notes")]
public class ClinicalNotesController(ClinicalNoteService notes) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.EmrNoteView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClinicalNoteDto>>>> Get(long patientId, CancellationToken ct) =>
        OkResponse(await notes.GetAsync(patientId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.EmrNoteManage)]
    public async Task<ActionResult<ApiResponse<long>>> Add(long patientId, AddClinicalNoteRequest request, CancellationToken ct)
    {
        var id = await notes.AddAsync(patientId, request, ct);
        return CreatedResponse($"/api/v1/emr/patients/{patientId}/clinical-notes/{id}", id, "Clinical note added.");
    }
}
