using System.ComponentModel.DataAnnotations;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Application.DTOs;

public sealed class UpdateInventoryRequest
{
    [Required]
    public Guid BlanketId { get; set; }

    [Required]
    public InventoryOwnerType OwnerType { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Range(0, 100000)]
    public int Quantity { get; set; }
}

public sealed class TransferInventoryRequest
{
    [Required]
    public Guid BlanketId { get; set; }

    [Required]
    public InventoryOwnerType FromOwnerType { get; set; }

    [Required]
    public Guid FromOwnerUserId { get; set; }

    [Required]
    public InventoryOwnerType ToOwnerType { get; set; }

    [Required]
    public Guid ToOwnerUserId { get; set; }

    [Range(1, 100000)]
    public int Quantity { get; set; }
}

public sealed class InventoryDto
{
    public Guid Id { get; set; }
    public Guid BlanketId { get; set; }
    public string BlanketModelName { get; set; } = string.Empty;
    public InventoryOwnerType OwnerType { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class StockCheckResponse
{
    public Guid BlanketId { get; set; }
    public InventoryOwnerType OwnerType { get; set; }
    public Guid OwnerUserId { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsAvailable { get; set; }
}
