using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.AppointmentTypes;

namespace Prixo.Api.Controllers.Masters;

/// <summary>"Appointment Masters" — a configurable categorization tag, bespoke since it carries DefaultSlotMinutes.</summary>
[Route("api/v1/appointment-types")]
public class AppointmentTypesController(AppointmentTypeService types) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AppointmentTypeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AppointmentTypeDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await types.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.AppointmentTypeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveAppointmentTypeRequest request, CancellationToken ct)
    {
        var id = await types.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/appointment-types/{id}", id, "Appointment type created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.AppointmentTypeManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveAppointmentTypeRequest request, CancellationToken ct)
    {
        await types.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Appointment type updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.AppointmentTypeManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await types.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Appointment type deleted.");
    }
}
