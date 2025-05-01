using Ledgerly.Application.Abstractions;
using Ledgerly.Infrastructure.Persistence;
using Ledgerly.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["CONNECTION_STRING"]
            ?? "Host=localhost;Port=5432;Database=ledgerly;Username=ledgerly;Password=ledgerly_dev";

        services.AddDbContext<LedgerlyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IJournalRepository, JournalRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();

        return services;
    }
}
