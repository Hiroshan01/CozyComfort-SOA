using CozyComfort.API.Extensions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
        => Ok(await userService.GetProfileAsync(User.GetUserId(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserProfileDto>>> GetUsers(CancellationToken cancellationToken)
        => Ok(await userService.GetUsersAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId:guid}/role")]
    public async Task<ActionResult<UserProfileDto>> UpdateRole(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
        => Ok(await userService.UpdateRoleAsync(userId, request, cancellationToken));
}
