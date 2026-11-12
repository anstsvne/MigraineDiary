using System.Security.Claims;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vital-measurements")]
public sealed class VitalMeasurementsController : ControllerBase
{
    private readonly VitalMeasurementService _vitalMeasurementService;

    public VitalMeasurementsController(
        VitalMeasurementService vitalMeasurementService)
    {
        _vitalMeasurementService = vitalMeasurementService;
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyCollection<VitalMeasurementResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var measurements =
            await _vitalMeasurementService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(measurements);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VitalMeasurementResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var measurement =
            await _vitalMeasurementService.GetByIdAsync(
                userId,
                id,
                cancellationToken);

        if (measurement is null)
        {
            return NotFound();
        }

        return Ok(measurement);
    }

    [HttpPost]
    public async Task<ActionResult<VitalMeasurementResponse>> Create(
    CreateVitalMeasurementRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var measurement =
            await _vitalMeasurementService.CreateAsync(
                userId,
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = measurement.Id
            },
            measurement);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VitalMeasurementResponse>> Update(
    Guid id,
    UpdateVitalMeasurementRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var measurement =
            await _vitalMeasurementService.UpdateAsync(
                userId,
                id,
                request,
                cancellationToken);

        if (measurement is null)
        {
            return NotFound();
        }

        return Ok(measurement);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await _vitalMeasurementService.DeleteAsync(
                userId,
                id,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private bool TryGetUserId(
        out Guid userId)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out userId);
    }
}