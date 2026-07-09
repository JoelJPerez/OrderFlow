using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

public class OrderItem : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // snapshot del precio al momento del pedido
    public decimal LineTotal => Quantity * UnitPrice;
}
