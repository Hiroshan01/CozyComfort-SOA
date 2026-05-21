using CozyComfort.API.Extensions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize(Roles = "Distributor")]
[Route("api/distributors")]
public sealed class DistributorsController(IDistributorService distributorService) : ControllerBase
{
    [HttpGet("assigned-sellers")]
    public async Task<ActionResult<IReadOnlyCollection<AssignedSellerDto>>> GetAssignedSellers(CancellationToken cancellationToken)
        => Ok(await distributorService.GetAssignedSellersAsync(User.GetUserId(), cancellationToken));

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetDistributorOrders(CancellationToken cancellationToken)
        => Ok(await distributorService.GetDistributorOrdersAsync(User.GetUserId(), cancellationToken));

    [HttpPost("orders/{orderId:guid}/fulfill")]
    public async Task<ActionResult<OrderDto>> FulfillSellerRequest(Guid orderId, CancellationToken cancellationToken)
        => Ok(await distributorService.FulfillSellerRequestAsync(User.GetUserId(), orderId, cancellationToken));
}
