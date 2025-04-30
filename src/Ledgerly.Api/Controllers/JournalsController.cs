using Ledgerly.Application.Journals;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/journals")]
public sealed class JournalsController : ControllerBase
{
    private readonly JournalService _journalService;

    public JournalsController(JournalService journalService)
    {
        _journalService = journalService;
    }

    [HttpGet("{journalEntryId:guid}")]
    public async Task<IActionResult> GetById(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var result = await _journalService.GetByIdAsync(organizationId, journalEntryId, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft(
        Guid organizationId,
        [FromBody] CreateJournalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId != organizationId)
        {
            return BadRequest(new { error = "OrganizationId in route and body must match." });
        }

        var result = await _journalService.CreateDraftAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, journalEntryId = result.Value!.Id },
            result.Value);
    }

    [HttpPost("{journalEntryId:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid organizationId,
        Guid journalEntryId,
        [FromBody] AddJournalLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _journalService.AddLineAsync(
            organizationId,
            journalEntryId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Journal entry not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{journalEntryId:guid}/post")]
    public async Task<IActionResult> Post(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var result = await _journalService.PostAsync(organizationId, journalEntryId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Journal entry not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{journalEntryId:guid}/reverse")]
    public async Task<IActionResult> Reverse(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var result = await _journalService.ReverseAsync(organizationId, journalEntryId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Journal entry not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
