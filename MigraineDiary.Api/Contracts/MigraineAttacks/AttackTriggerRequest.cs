using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackTriggerRequest
{
    [Required]
    public Guid TriggerId { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }
}