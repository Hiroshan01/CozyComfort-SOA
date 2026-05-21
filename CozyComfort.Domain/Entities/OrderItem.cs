using CozyComfort.Domain.Common;

namespace CozyComfort.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid BlanketId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;
    public Blanket Blanket { get; set; } = null!;
}
