using MigraineDiary.Api.Contracts.Symptoms;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/symptoms")]
public sealed class SymptomsController : ControllerBase
{
    private readonly SymptomService _symptomService;

    public SymptomsController(
        SymptomService symptomService)
    {
        _symptomService = symptomService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SymptomResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result =
            await _symptomService.GetAllAsync(
                cancellationToken);

        return Ok(result);
    }
}