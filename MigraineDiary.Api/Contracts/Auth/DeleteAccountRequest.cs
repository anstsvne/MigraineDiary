using System.ComponentModel.DataAnnotations;

namespace MigraineDiary.Api.Contracts.Auth;

public sealed class DeleteAccountRequest
{
    [Required(ErrorMessage = "Пароль обязателен.")]
    public string Password { get; init; } = string.Empty;
}
