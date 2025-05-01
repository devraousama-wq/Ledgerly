using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly LedgerlyDbContext _context;

    public ContactRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<Contact?> GetByIdAsync(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default) =>
        _context.Contacts
            .FirstOrDefaultAsync(
                contact => contact.OrganizationId == organizationId && contact.Id == contactId,
                cancellationToken);

    public async Task<IReadOnlyList<Contact>> ListByTypeAsync(
        Guid organizationId,
        ContactType contactType,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contacts
            .Where(contact =>
                contact.OrganizationId == organizationId &&
                contact.ContactType == contactType);

        if (!includeArchived)
        {
            query = query.Where(contact => !contact.IsArchived);
        }

        return await query
            .OrderBy(contact => contact.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await _context.Contacts.AddAsync(contact, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        _context.Contacts.Update(contact);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
