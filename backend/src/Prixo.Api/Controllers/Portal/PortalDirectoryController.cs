using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Branches;
using Prixo.Application.Features.Masters.Doctors;

namespace Prixo.Api.Controllers.Portal;

/// <summary>Read-only branch/doctor lookups the portal booking screen needs — reuses the exact same staff services (BranchService/DoctorService), no parallel directory.</summary>
[Route("api/v1/portal")]
[PortalAuth]
public class PortalDirectoryController(BranchService branches, DoctorService doctors) : ApiControllerBase
{
    [HttpGet("branches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchDto>>>> GetBranches(CancellationToken ct) =>
        OkResponse(await branches.GetAllAsync(ct));

    [HttpGet("doctors")]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorListItemDto>>>> GetDoctors([FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await doctors.GetPagedAsync(new PagedQuery { Page = 1, PageSize = 100, Search = search }, ct));
}
