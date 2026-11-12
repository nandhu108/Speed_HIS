using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.PatientCategories;

namespace Prixo.Api.Controllers.Masters;

/// <summary>"Patient-type colors" — bespoke since it carries ColorHex. Route auto-derives to
/// api/v1/patientcategories via [controller], matching the pre-existing endpoint.</summary>
public class PatientCategoriesController(PatientCategoryService categories) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PatientCategoryView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientCategoryDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await categories.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PatientCategoryManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SavePatientCategoryRequest request, CancellationToken ct)
    {
        var id = await categories.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/patientcategories/{id}", id, "Patient category created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PatientCategoryManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SavePatientCategoryRequest request, CancellationToken ct)
    {
        await categories.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Patient category updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.PatientCategoryManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Patient category deleted.");
    }
}
