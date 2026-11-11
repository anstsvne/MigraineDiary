using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Medications;

public sealed class CreateMedicationRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}