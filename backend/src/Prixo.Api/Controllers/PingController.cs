using Microsoft.AspNetCore.Mvc;

namespace Prixo.Api.Controllers;

/// <summary>
/// Smoke-test endpoint proving routing, the response envelope, and middleware wiring.
/// </summary>
public class PingController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        OkResponse(new { name = "Prixo HIS API", version = "0.1.0", serverTime = DateTimeOffset.UtcNow });
}
