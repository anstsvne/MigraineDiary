namespace MigraineDiary.Api.Contracts.Medications;

public sealed class MedicationResponse
{
    public Guid Id { get; init; }

    public required string Name { get; init; }
}