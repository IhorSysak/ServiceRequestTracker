using Microsoft.AspNetCore.Mvc;

namespace ServiceRequestTracker.Features.ServiceRequests;

[ApiController]
[Route("api/service-requests")]
public class ServiceRequestsController(IServiceRequestService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateServiceRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await service.GetByIdAsync(id, cancellationToken);

    [HttpGet]
    [ProducesResponseType<PagedResponse<ServiceRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ServiceRequestResponse>>> List(
        [FromQuery] ListServiceRequestsQuery query, CancellationToken cancellationToken) =>
        await service.ListAsync(query, cancellationToken);

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceRequestResponse>> ChangeStatus(
        Guid id, ChangeStatusRequest request, CancellationToken cancellationToken) =>
        await service.ChangeStatusAsync(id, request.Status!.Value, cancellationToken);
}