using Ledgerly.Application.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();

        return services;
    }
}
