using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Ledgerly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class BankStatementRepository : IBankStatementRepository
{
    private readonly LedgerlyDbContext _context;

    public BankStatementRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<BankStatement?> GetByIdAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default) =>
        _context.BankStatements
            .Include(statement => statement.Lines)
            .FirstOrDefaultAsync(
                statement => statement.OrganizationId == organizationId && statement.Id == statementId,
                cancellationToken);

    public async Task<IReadOnlyList<BankStatement>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BankStatements
            .Include(statement => statement.Lines)
            .Where(statement => statement.OrganizationId == organizationId)
            .OrderByDescending(statement => statement.PeriodEnd)
            .ThenByDescending(statement => statement.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListMatchedJournalLineIdsAsync(
        Guid organizationId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.BankStatementLines
            .Where(line =>
                line.MatchedJournalLineId.HasValue &&
                _context.BankStatements.Any(statement =>
                    statement.Id == line.BankStatementId &&
                    statement.OrganizationId == organizationId &&
                    statement.BankAccountId == bankAccountId))
            .Select(line => line.MatchedJournalLineId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BankStatement statement, CancellationToken cancellationToken = default)
    {
        await _context.BankStatements.AddAsync(statement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BankStatement statement, CancellationToken cancellationToken = default)
    {
        _context.BankStatements.Update(statement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
