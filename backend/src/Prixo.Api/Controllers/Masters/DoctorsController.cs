using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Doctors;

namespace Prixo.Api.Controllers.Masters;

public class DoctorsController(DoctorService doctors) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DoctorView)]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorListItemDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await doctors.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.DoctorView)]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await doctors.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DoctorManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveDoctorRequest request, CancellationToken ct)
    {
        var id = await doctors.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/doctors/{id}", id, "Doctor created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DoctorManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveDoctorRequest request, CancellationToken ct)
    {
        await doctors.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Doctor updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DoctorManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await doctors.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Doctor deleted.");
    }
}
