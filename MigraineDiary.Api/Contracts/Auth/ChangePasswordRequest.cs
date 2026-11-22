using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Auth;

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Текущий пароль обязателен.")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен.")]
    [MinLength(
        8,
        ErrorMessage = "Пароль должен содержать минимум 8 символов.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение нового пароля обязательно.")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Новый пароль и подтверждение пароля не совпадают.")]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
