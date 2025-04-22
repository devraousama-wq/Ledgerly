namespace Ledgerly.Domain.Common;

public abstract class OrganizationScopedEntity : Entity
{
    public Guid OrganizationId { get; protected set; }

    protected OrganizationScopedEntity(Guid organizationId)
    {
        OrganizationId = organizationId;
    }

    protected OrganizationScopedEntity()
    {
    }
}
