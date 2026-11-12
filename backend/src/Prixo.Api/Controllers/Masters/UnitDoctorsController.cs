using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Units;

namespace Prixo.Api.Controllers.Masters;

/// <summary>"Link Unit &amp; Doctors" — reuses MASTER.UNIT.{VIEW,MANAGE} since this is a Unit-scoped sub-resource, not a new module.</summary>
[Route("api/v1/units/{unitId:long}/doctors")]
public class UnitDoctorsController(UnitDoctorService unitDoctors) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.UnitView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnitDoctorDto>>>> Get(long unitId, CancellationToken ct) =>
        OkResponse(await unitDoctors.GetByUnitIdAsync(unitId, ct));

    [HttpPut]
    [HasPermission(PermissionRegistry.UnitManage)]
    public async Task<ActionResult<ApiResponse>> ReplaceDoctors(
        long unitId, ReplaceUnitDoctorsRequest request, CancellationToken ct)
    {
        await unitDoctors.ReplaceDoctorsAsync(unitId, request, ct);
        return OkResponse<object?>(null, "Linked doctors updated.");
    }
}
