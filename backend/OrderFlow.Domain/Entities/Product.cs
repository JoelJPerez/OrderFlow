using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

public class Product : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public InventoryItem? Inventory { get; set; }
}