using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Entities;

public class InventoryItem : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0 || QuantityAvailable < quantity) return false;
        QuantityReserved += quantity;
        return true;
    }

    public void ReleaseReservation(int quantity) =>
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);

    public void CommitReservation(int quantity)
    {
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
        QuantityOnHand = Math.Max(0, QuantityOnHand - quantity);
    }
}
