namespace MigraineDiary.Api.Contracts.Auth;

public sealed class RegisterResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}