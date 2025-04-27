using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Ledgerly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly LedgerlyDbContext _context;

    public AccountRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<Account?> GetByIdAsync(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        _context.Accounts
            .FirstOrDefaultAsync(
                account => account.OrganizationId == organizationId && account.Id == accountId,
                cancellationToken);

    public Task<Account?> GetByCodeAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default) =>
        _context.Accounts
            .FirstOrDefaultAsync(
                account => account.OrganizationId == organizationId && account.Code == code,
                cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Accounts
            .Where(account => account.OrganizationId == organizationId);

        if (!includeArchived)
        {
            query = query.Where(account => !account.IsArchived);
        }

        return await query
            .OrderBy(account => account.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetChildrenAsync(
        Guid organizationId,
        Guid parentAccountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Where(account =>
                account.OrganizationId == organizationId &&
                account.ParentAccountId == parentAccountId)
            .OrderBy(account => account.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveChildrenAsync(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        _context.Accounts.AnyAsync(
            account =>
                account.OrganizationId == organizationId &&
                account.ParentAccountId == accountId &&
                !account.IsArchived,
            cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
