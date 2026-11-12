using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/admissions/{admissionId:long}/file-audit")]
public class FileAuditsController(FileAuditService audits) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdAuditView)]
    public async Task<ActionResult<ApiResponse<FileAuditDto>>> Get(long admissionId, CancellationToken ct) =>
        OkResponse(await audits.GetByAdmissionAsync(admissionId, ct));

    [HttpPut]
    [HasPermission(PermissionRegistry.MrdAuditManage)]
    public async Task<ActionResult<ApiResponse<long>>> Save(long admissionId, SaveFileAuditRequest request, CancellationToken ct) =>
        OkResponse(await audits.SaveAsync(admissionId, request, ct), "File audit saved.");
}
