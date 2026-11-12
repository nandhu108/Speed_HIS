using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Insurance;

namespace Prixo.Api.Controllers.Masters;

/// <summary>Placeholder master for the held Insurance/TPA module — CRUD only.</summary>
[Route("api/v1/insurance-companies")]
public class InsuranceCompaniesController(InsuranceCompanyService companies) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.InsuranceView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InsuranceCompanyDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await companies.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.InsuranceManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveInsuranceCompanyRequest request, CancellationToken ct)
    {
        var id = await companies.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/insurance-companies/{id}", id, "Insurance company created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.InsuranceManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveInsuranceCompanyRequest request, CancellationToken ct)
    {
        await companies.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Insurance company updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.InsuranceManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await companies.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Insurance company deleted.");
    }
}
