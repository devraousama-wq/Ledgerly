using Ledgerly.Application.Contacts;
using Ledgerly.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly ContactService _contactService;

    public ContactsController(ContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] ContactType contactType,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _contactService.ListByTypeAsync(
            organizationId,
            contactType,
            includeArchived,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{contactId:guid}")]
    public async Task<IActionResult> GetById(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var result = await _contactService.GetByIdAsync(organizationId, contactId, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid organizationId,
        [FromBody] CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId != organizationId)
        {
            return BadRequest(new { error = "OrganizationId in route and body must match." });
        }

        var result = await _contactService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, contactId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{contactId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        Guid contactId,
        [FromBody] UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _contactService.UpdateAsync(
            organizationId,
            contactId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Contact not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{contactId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var result = await _contactService.ArchiveAsync(organizationId, contactId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Contact not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
