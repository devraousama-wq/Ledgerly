namespace Ledgerly.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void Entity_assigns_unique_id()
    {
        var first = new TestEntity();
        var second = new TestEntity();
        Assert.NotEqual(first.Id, second.Id);
    }

    private sealed class TestEntity : Ledgerly.Domain.Common.Entity
    {
    }
}
