namespace MigraineDiary.Api.Contracts.Symptoms;

public sealed class SymptomResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }
}