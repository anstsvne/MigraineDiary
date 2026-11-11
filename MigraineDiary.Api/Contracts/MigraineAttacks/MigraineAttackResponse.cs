using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackResponse
{
    public Guid Id { get; init; }

    public DateTimeOffset StartAt { get; init; }

    public DateTimeOffset EndAt { get; init; }

    public double DurationMinutes { get; init; }

    public int Intensity { get; init; }

    public PainLocation PainLocation { get; init; }

    public string? PainLocationDescription { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyCollection<AttackSymptomResponse> Symptoms { get; init; }
    = Array.Empty<AttackSymptomResponse>();

    public IReadOnlyCollection<AttackTriggerResponse> Triggers { get; init; }
    = Array.Empty<AttackTriggerResponse>();

    public IReadOnlyCollection<AttackMedicationResponse> Medications { get; init; }
    = Array.Empty<AttackMedicationResponse>();
}