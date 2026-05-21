using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize(Roles = "Manufacturer,Admin")]
[Route("api/manufacturers")]
public sealed class ManufacturersController(IManufacturerService manufacturerService) : ControllerBase
{
    [HttpGet("production-capacity")]
    public async Task<ActionResult<ProductionCapacityResponse>> CheckProductionCapacity(
        [FromQuery] Guid blanketId,
        [FromQuery] int requestedQuantity,
        CancellationToken cancellationToken)
        => Ok(await manufacturerService.CheckProductionCapacityAsync(blanketId, requestedQuantity, cancellationToken));

    [HttpPut("production-status/{blanketId:guid}")]
    public async Task<ActionResult<BlanketDto>> UpdateProductionStatus(Guid blanketId, UpdateProductionStatusRequest request, CancellationToken cancellationToken)
        => Ok(await manufacturerService.UpdateProductionStatusAsync(blanketId, request, cancellationToken));

    [HttpGet("lead-time")]
    public async Task<ActionResult<LeadTimeResponse>> ProvideLeadTime(
        [FromQuery] Guid blanketId,
        [FromQuery] int requestedQuantity,
        CancellationToken cancellationToken)
        => Ok(await manufacturerService.ProvideLeadTimeAsync(blanketId, requestedQuantity, cancellationToken));
}
