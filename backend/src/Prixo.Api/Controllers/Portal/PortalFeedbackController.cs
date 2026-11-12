using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Portal;

namespace Prixo.Api.Controllers.Portal;

[Route("api/v1/portal/feedback")]
[PortalAuth]
public class PortalFeedbackController(PortalFeedbackService feedback) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FeedbackDto>>>> GetMine(CancellationToken ct) =>
        OkResponse(await feedback.GetMineAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Submit(SubmitFeedbackRequest request, CancellationToken ct)
    {
        var id = await feedback.SubmitAsync(request, ct);
        return CreatedResponse($"/api/v1/portal/feedback/{id}", id, "Thank you for your feedback.");
    }
}
