using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InventoryDto>>> GetInventory(CancellationToken cancellationToken)
        => Ok(await inventoryService.GetInventoryAsync(cancellationToken));

    [HttpGet("check")]
    public async Task<ActionResult<StockCheckResponse>> CheckStock(
        [FromQuery] Guid blanketId,
        [FromQuery] InventoryOwnerType ownerType,
        [FromQuery] Guid ownerUserId,
        CancellationToken cancellationToken)
        => Ok(await inventoryService.CheckStockAsync(blanketId, ownerType, ownerUserId, cancellationToken));

    [Authorize(Roles = "Manufacturer,Distributor,Admin")]
    [HttpPut]
    public async Task<ActionResult<InventoryDto>> UpdateInventory(UpdateInventoryRequest request, CancellationToken cancellationToken)
        => Ok(await inventoryService.UpdateInventoryAsync(request, cancellationToken));

    [Authorize(Roles = "Manufacturer,Distributor,Admin")]
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferInventory(TransferInventoryRequest request, CancellationToken cancellationToken)
    {
        await inventoryService.TransferInventoryAsync(request, cancellationToken);
        return NoContent();
    }
}
