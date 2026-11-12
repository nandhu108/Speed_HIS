using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/contracts")]
public class AmcContractsController(AmcContractService contracts) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedContractView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AmcContractDto>>>> GetAll(
        [FromQuery] long? equipmentId, [FromQuery] bool renewalDueOnly, CancellationToken ct) =>
        OkResponse(await contracts.GetAllAsync(equipmentId, renewalDueOnly, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedContractManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveAmcContractRequest request, CancellationToken ct)
    {
        var id = await contracts.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/contracts/{id}", id, "Contract created.");
    }
}
