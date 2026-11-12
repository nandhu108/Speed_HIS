using Microsoft.AspNetCore.Mvc;
using Prixo.Application.Common.Models;

namespace Prixo.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected OkObjectResult OkResponse<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected CreatedResult CreatedResponse<T>(string location, T data, string? message = null) =>
        Created(location, ApiResponse<T>.Ok(data, message));
}
