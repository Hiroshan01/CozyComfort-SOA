using CozyComfort.Domain.Common;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Domain.Entities;

public sealed class InventoryRecord : BaseEntity
{
    public Guid BlanketId { get; set; }
    public InventoryOwnerType OwnerType { get; set; }
    public Guid OwnerUserId { get; set; }
    public int Quantity { get; set; }

    public Blanket Blanket { get; set; } = null!;
    public User OwnerUser { get; set; } = null!;
}
