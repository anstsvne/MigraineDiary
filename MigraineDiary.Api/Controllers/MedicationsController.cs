using System.Security.Claims;
using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/medications")]
public sealed class MedicationsController : ControllerBase
{
    private readonly MedicationService _medicationService;

    public MedicationsController(
        MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedicationResponse>>> GetAll(
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
            await _medicationService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MedicationResponse>> Create(
    CreateMedicationRequest request,
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
            await _medicationService.CreateAsync(
                userId,
                request,
                cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }
}