using Ledgerly.Application.Recurring;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/recurring-schedules")]
public sealed class RecurringController : ControllerBase
{
    private readonly RecurringScheduleService _service;

    public RecurringController(RecurringScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, CancellationToken ct = default)
    {
        var r = await _service.ListAsync(organizationId, ct);
        return r.IsFailure ? BadRequest(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid scheduleId, CancellationToken ct = default)
    {
        var r = await _service.GetByIdAsync(organizationId, scheduleId, ct);
        return r.IsFailure ? NotFound(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateRecurringScheduleRequest req, CancellationToken ct = default)
    {
        if (req.OrganizationId != organizationId)
            return BadRequest(new { error = "Organization mismatch." });

        var r = await _service.CreateAsync(req, ct);
        return r.IsFailure ? BadRequest(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpPut("{scheduleId:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid scheduleId, [FromBody] UpdateRecurringScheduleRequest req, CancellationToken ct = default)
    {
        var r = await _service.UpdateAsync(organizationId, scheduleId, req, ct);
        return r.IsFailure ? BadRequest(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpPost("{scheduleId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid organizationId, Guid scheduleId, CancellationToken ct = default)
    {
        var r = await _service.PauseAsync(organizationId, scheduleId, ct);
        return r.IsFailure ? NotFound(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpPost("{scheduleId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid organizationId, Guid scheduleId, CancellationToken ct = default)
    {
        var r = await _service.ResumeAsync(organizationId, scheduleId, ct);
        return r.IsFailure ? NotFound(new { error = r.Error }) : Ok(r.Value);
    }

    [HttpPost("preview")]
    public IActionResult Preview([FromBody] PreviewRecurringScheduleRequest req)
    {
        var r = _service.PreviewAsync(req);
        return r.IsFailure ? BadRequest(new { error = r.Error }) : Ok(r.Value);
    }
}
