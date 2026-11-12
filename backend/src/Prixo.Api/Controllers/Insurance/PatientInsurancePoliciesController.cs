using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Insurance;
using Prixo.Application.Features.Patients;

namespace Prixo.Api.Controllers.Insurance;

/// <summary>Read-only lookup for the Pre-Auth/Claim raise screens — policies are captured inline on Patient registration/update.</summary>
[Route("api/v1/patients/{patientId:long}/insurance-policies")]
public class PatientInsurancePoliciesController(PatientInsurancePolicyService policies) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.InsurancePolicyView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientInsurancePolicyDto>>>> GetByPatient(
        long patientId, CancellationToken ct) =>
        OkResponse(await policies.GetByPatientAsync(patientId, ct));
}
