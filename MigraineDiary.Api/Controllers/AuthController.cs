using System.Security.Claims;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigraineDiary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(
        AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _authService.RegisterAsync(
                request,
                cancellationToken);

        return Ok(result);
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
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _authService.GetMeAsync(
                userId,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<MeResponse>> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _authService.UpdateProfileAsync(
                userId,
                request,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var changed =
            await _authService.ChangePasswordAsync(
                userId,
                request,
                cancellationToken);

        if (!changed)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(
        DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await _authService.DeleteAccountAsync(
                userId,
                request,
                cancellationToken);

        if (!deleted)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    private bool TryGetUserId(
        out Guid userId)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out userId);
    }
}
