using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MigraineAttackFilterRequest
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    [Range(1, 10)]
    public int? MinIntensity { get; init; }

    [Range(1, 10)]
    public int? MaxIntensity { get; init; }

    public PainLocation? PainLocation { get; init; }

    public Guid? SymptomId { get; init; }

    public Guid? TriggerId { get; init; }

    public Guid? MedicationId { get; init; }
}