using MigraineDiary.Api.Contracts.Triggers;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/triggers")]
public sealed class TriggersController : ControllerBase
{
    private readonly TriggerService _triggerService;

    public TriggersController(
        TriggerService triggerService)
    {
        _triggerService = triggerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TriggerResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result =
            await _triggerService.GetAllAsync(
                cancellationToken);

        return Ok(result);
    }
}