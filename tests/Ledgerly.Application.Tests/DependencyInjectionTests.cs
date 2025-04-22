using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_returns_services()
    {
        var services = new ServiceCollection();
        var result = Ledgerly.Application.DependencyInjection.AddApplication(services);
        Assert.Same(services, result);
    }
}
