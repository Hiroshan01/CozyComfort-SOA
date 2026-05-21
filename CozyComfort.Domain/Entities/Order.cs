using CozyComfort.Domain.Common;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Domain.Entities;

public sealed class Order : BaseEntity
{
    public Guid SellerId { get; set; }
    public Guid DistributorId { get; set; }
    public Guid? ManufacturerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Notes { get; set; }
    public int EstimatedLeadTimeDays { get; set; }

    public User Seller { get; set; } = null!;
    public User Distributor { get; set; } = null!;
    public User? Manufacturer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
