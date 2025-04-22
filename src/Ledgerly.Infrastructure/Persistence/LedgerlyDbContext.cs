using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence;

public sealed class LedgerlyDbContext : DbContext
{
    public LedgerlyDbContext(DbContextOptions<LedgerlyDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerlyDbContext).Assembly);
    }
}
