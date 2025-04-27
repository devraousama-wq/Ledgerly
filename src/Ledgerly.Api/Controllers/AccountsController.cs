using Ledgerly.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountsController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.ListByOrganizationAsync(
            organizationId,
            includeArchived,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{accountId:guid}")]
    public async Task<IActionResult> GetById(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.GetByIdAsync(organizationId, accountId, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid organizationId,
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId != organizationId)
        {
            return BadRequest(new { error = "OrganizationId in route and body must match." });
        }

        var result = await _accountService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, accountId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{accountId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        Guid accountId,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.UpdateAsync(
            organizationId,
            accountId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Account not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{accountId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountService.ArchiveAsync(organizationId, accountId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Account not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
