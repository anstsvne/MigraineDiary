namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackMedicationResponse
{
    public Guid Id { get; init; }

    public Guid MedicationId { get; init; }

    public required string Name { get; init; }

    public string? Dosage { get; init; }

    public DateTimeOffset? TakenAt { get; init; }
}