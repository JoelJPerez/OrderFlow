using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

public class Order : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Draft,      // creado, stock reservado
    Confirmed,  // reserva convertida en descuento de stock
    Shipped,
    Delivered,
    Cancelled   // reserva liberada
}
