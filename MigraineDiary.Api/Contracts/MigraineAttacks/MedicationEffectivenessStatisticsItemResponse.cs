using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class MedicationEffectivenessStatisticsItemResponse
{
    public Guid MedicationId { get; init; }

    public string MedicationName { get; init; } = string.Empty;

    public MedicationEffectiveness Effectiveness { get; init; }

    public int Count { get; init; }
}