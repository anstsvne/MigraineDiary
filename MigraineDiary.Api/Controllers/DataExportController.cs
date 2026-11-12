using System.Security.Claims;
using MigraineDiary.Api.Contracts.DataExport;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Route("api/data-export")]
[Authorize]
public sealed class DataExportController : ControllerBase
{
    private readonly DataExportService _dataExportService;

    public DataExportController(
        DataExportService dataExportService)
    {
        _dataExportService = dataExportService;
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] DataExportRequest request,
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
            var content =
                await _dataExportService.ExportAsync(
                    userId,
                    request,
                    cancellationToken);

            var fileName =
                $"migraine-diary-export-" +
                $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip";

            return File(
                content,
                "application/zip",
                fileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}