namespace OrderFlow.Domain.Common;

/// <summary>
/// Entidades que pertenecen a un tenant. EF Core aplicará
/// un global query filter por TenantId automáticamente.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
