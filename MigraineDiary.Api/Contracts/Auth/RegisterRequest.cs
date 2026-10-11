using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Имя обязательно.")]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный email.")]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение пароля обязательно.")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Пароль и подтверждение пароля не совпадают.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}