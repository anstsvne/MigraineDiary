using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class UpdateMigraineAttackRequest
{
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public DateTimeOffset? StartAt { get; init; }

    public DateTimeOffset? EndAt { get; init; }

    [Range(1, 10)]
    public int Intensity { get; init; }

    [Required]
    [EnumDataType(typeof(PainLocation))]
    public PainLocation? PainLocation { get; init; }

    [MaxLength(500)]
    public string? PainLocationDescription { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyCollection<AttackSymptomRequest> Symptoms { get; init; }
        = Array.Empty<AttackSymptomRequest>();

    public IReadOnlyCollection<AttackTriggerRequest> Triggers { get; init; }
        = Array.Empty<AttackTriggerRequest>();

    public IReadOnlyCollection<AttackMedicationRequest> Medications { get; init; }
        = Array.Empty<AttackMedicationRequest>();
}