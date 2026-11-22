using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Auth;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Имя обязательно.")]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный email.")]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;
}
