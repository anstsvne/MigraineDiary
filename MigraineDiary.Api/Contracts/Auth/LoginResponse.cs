namespace MigraineDiary.Api.Contracts.Auth;

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public Guid UserId { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}