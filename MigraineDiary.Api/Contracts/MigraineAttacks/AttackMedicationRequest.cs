using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackMedicationRequest
{
    [Required]
    public Guid MedicationId { get; init; }

    [MaxLength(100)]
    public string? Dosage { get; init; }

    public DateTimeOffset? TakenAt { get; init; }

    // Existing intake identifier; omitted for new intakes.
    public Guid? Id { get; init; }

    [EnumDataType(typeof(MedicationEffectiveness))]
    public MedicationEffectiveness? Effectiveness { get; init; }
}