using CozyComfort.API.Extensions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [Authorize(Roles = "Seller")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateOrderAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(TrackOrder), new { orderId = order.Id }, order);
    }

    [HttpPut("{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> UpdateOrder(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken)
        => Ok(await orderService.UpdateOrderAsync(User.GetUserId(), orderId, request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetOrders(CancellationToken cancellationToken)
        => Ok(await orderService.GetOrdersAsync(User.GetUserId(), cancellationToken));

    [HttpGet("{orderId:guid}/track")]
    public async Task<ActionResult<TrackOrderResponse>> TrackOrder(Guid orderId, CancellationToken cancellationToken)
        => Ok(await orderService.TrackOrderAsync(User.GetUserId(), orderId, cancellationToken));
}
