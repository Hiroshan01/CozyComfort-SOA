using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize]
[Route("api/blankets")]
public sealed class BlanketsController(IBlanketService blanketService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<BlanketDto>>> GetBlankets(CancellationToken cancellationToken)
        => Ok(await blanketService.GetBlanketsAsync(cancellationToken));

    [HttpGet("{blanketId:guid}")]
    public async Task<ActionResult<BlanketDto>> GetBlanket(Guid blanketId, CancellationToken cancellationToken)
        => Ok(await blanketService.GetBlanketByIdAsync(blanketId, cancellationToken));

    [Authorize(Roles = "Manufacturer,Admin")]
    [HttpPost]
    public async Task<ActionResult<BlanketDto>> CreateBlanket(UpsertBlanketRequest request, CancellationToken cancellationToken)
    {
        var blanket = await blanketService.CreateBlanketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBlanket), new { blanketId = blanket.Id }, blanket);
    }

    [Authorize(Roles = "Manufacturer,Admin")]
    [HttpPut("{blanketId:guid}")]
    public async Task<ActionResult<BlanketDto>> UpdateBlanket(Guid blanketId, UpsertBlanketRequest request, CancellationToken cancellationToken)
        => Ok(await blanketService.UpdateBlanketAsync(blanketId, request, cancellationToken));

    [Authorize(Roles = "Manufacturer,Admin")]
    [HttpDelete("{blanketId:guid}")]
    public async Task<IActionResult> DeleteBlanket(Guid blanketId, CancellationToken cancellationToken)
    {
        await blanketService.DeleteBlanketAsync(blanketId, cancellationToken);
        return NoContent();
    }
}
