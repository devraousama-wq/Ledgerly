using Ledgerly.Application.Accounts;
using Ledgerly.Application.Journals;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<JournalService>();

        return services;
    }
}
