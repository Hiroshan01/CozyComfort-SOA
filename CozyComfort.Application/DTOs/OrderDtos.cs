using System.ComponentModel.DataAnnotations;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Application.DTOs;

public sealed class CreateOrderRequest
{
    [Required, StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MinLength(1)]
    public List<OrderItemRequest> Items { get; set; } = new();
}

public sealed class OrderItemRequest
{
    [Required]
    public Guid BlanketId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public sealed class UpdateOrderRequest
{
    public OrderStatus? Status { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
}

public sealed class OrderItemDto
{
    public Guid BlanketId { get; set; }
    public string BlanketModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class OrderDto
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public Guid DistributorId { get; set; }
    public string DistributorName { get; set; } = string.Empty;
    public Guid? ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public int EstimatedLeadTimeDays { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public IReadOnlyCollection<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
}

public sealed class TrackOrderResponse
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public int EstimatedLeadTimeDays { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
