using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackMedicationRequest
{
    [Required]
    public Guid MedicationId { get; init; }

    [MaxLength(100)]
    public string? Dosage { get; init; }

    public DateTimeOffset? TakenAt { get; init; }
}