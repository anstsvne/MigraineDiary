using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _authService.RegisterAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
    LoginRequest request,
    CancellationToken cancellationToken)
    {
        var result =
            await _authService.LoginAsync(
                request,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized(new
            {
                message = "Неверный email или пароль."
            });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(
    CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.GetMeAsync(
            userId,
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }
}