using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.MigraineAttacks;

public sealed class AttackSymptomRequest
{
    [Required]
    public Guid SymptomId { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }
}