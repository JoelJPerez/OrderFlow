
namespace OrderFlow.Application.Common.Interfaces;

public interface ITenantProvider
{
    /// <summary>TenantId del usuario autenticado, o null si no hay contexto de tenant (ej. endpoints públicos).</summary>
    Guid? TenantId { get; }
}
