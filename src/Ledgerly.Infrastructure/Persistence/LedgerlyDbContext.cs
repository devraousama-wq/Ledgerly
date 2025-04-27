using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence;

public sealed class LedgerlyDbContext : DbContext
{
    public LedgerlyDbContext(DbContextOptions<LedgerlyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerlyDbContext).Assembly);
    }
}
