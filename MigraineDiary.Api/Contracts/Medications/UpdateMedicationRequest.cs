using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Medications;

public sealed class UpdateMedicationRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}
