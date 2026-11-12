using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/accounts")]
public class AccountsController(AccountService accounts) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.FinanceAccountView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await accounts.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.FinanceAccountManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveAccountRequest request, CancellationToken ct)
    {
        var id = await accounts.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/finance/accounts/{id}", id, "Account created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.FinanceAccountManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveAccountRequest request, CancellationToken ct)
    {
        await accounts.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Account updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.FinanceAccountManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await accounts.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Account deleted.");
    }
}
