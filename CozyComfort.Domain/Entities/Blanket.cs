using CozyComfort.Domain.Common;

namespace CozyComfort.Domain.Entities;

public sealed class Blanket : BaseEntity
{
    public string ModelName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ProductionCapacity { get; set; }
    public int CurrentStock { get; set; }

    public ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
