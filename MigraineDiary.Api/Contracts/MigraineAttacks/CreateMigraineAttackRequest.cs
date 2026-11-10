using System.ComponentModel.DataAnnotations;
using MigraineDiary.Api.Enums;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class CreateMigraineAttackRequest
{
    [Required]
    public DateTimeOffset? StartAt { get; init; }

    [Required]
    public DateTimeOffset? EndAt { get; init; }

    [Range(1, 10)]
    public int Intensity { get; init; }

    [Required]
    public PainLocation? PainLocation { get; init; }

    [MaxLength(500)]
    public string? PainLocationDescription { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyCollection<AttackSymptomRequest> Symptoms { get; init; }
    = Array.Empty<AttackSymptomRequest>();
}