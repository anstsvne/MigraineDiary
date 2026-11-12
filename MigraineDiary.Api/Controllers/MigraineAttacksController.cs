using System.Security.Claims;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/migraine-attacks")]
public sealed class MigraineAttacksController : ControllerBase
{
    private readonly MigraineAttackService _migraineAttackService;

    public MigraineAttacksController(
        MigraineAttackService migraineAttackService)
    {
        _migraineAttackService = migraineAttackService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MigraineAttackResponse>>> GetAll(
        [FromQuery] MigraineAttackFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _migraineAttackService.GetAllAsync(
                    userId,
                    filter,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<MigraineAttackStatisticsResponse>> GetStatistics(
    [FromQuery] MigraineAttackStatisticsRequest request,
    CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _migraineAttackService.GetStatisticsAsync(
                    userId,
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MigraineAttackResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _migraineAttackService.GetByIdAsync(
                userId,
                id,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MigraineAttackResponse>> Create(
        CreateMigraineAttackRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _migraineAttackService.CreateAsync(
                    userId,
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MigraineAttackResponse>> Update(
        Guid id,
        UpdateMigraineAttackRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _migraineAttackService.UpdateAsync(
                    userId,
                    id,
                    request,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await _migraineAttackService.DeleteAsync(
                userId,
                id,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}