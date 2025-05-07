using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IBankStatementRepository
{
    Task<BankStatement?> GetByIdAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankStatement>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListMatchedJournalLineIdsAsync(
        Guid organizationId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BankStatement statement, CancellationToken cancellationToken = default);

    Task UpdateAsync(BankStatement statement, CancellationToken cancellationToken = default);
}
