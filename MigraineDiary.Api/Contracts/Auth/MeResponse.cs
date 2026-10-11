namespace MigraineDiary.Api.Contracts.Auth;

public sealed class MeResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}