namespace Ledgerly.Integration.Tests;

public class ApiSmokeTests
{
    [Fact]
    public void Api_assembly_loads()
    {
        var assembly = typeof(Ledgerly.Api.Controllers.HealthController).Assembly;
        Assert.NotNull(assembly);
    }
}
