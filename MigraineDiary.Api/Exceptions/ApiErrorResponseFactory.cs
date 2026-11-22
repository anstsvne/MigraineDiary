using MigraineDiary.Api.Contracts.Common;

namespace MigraineDiary.Api.Exceptions;

public static class ApiErrorResponseFactory
{
    public static ApiErrorResponse Create(
        HttpContext httpContext,
        int status,
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            Status = status,
            Code = code,
            Message = message,
            Errors = errors,
            TraceId = httpContext.TraceIdentifier
        };
    }

    public static ApiErrorResponse CreateForStatus(
        HttpContext httpContext,
        int status)
    {
        var (code, message) =
            status switch
            {
                StatusCodes.Status400BadRequest =>
                    (
                        "bad_request",
                        "Некорректный запрос."
                    ),

                StatusCodes.Status401Unauthorized =>
                    (
                        "unauthorized",
                        "Требуется авторизация."
                    ),

                StatusCodes.Status403Forbidden =>
                    (
                        "forbidden",
                        "Доступ запрещён."
                    ),

                StatusCodes.Status404NotFound =>
                    (
                        "not_found",
                        "Ресурс не найден."
                    ),

                StatusCodes.Status405MethodNotAllowed =>
                    (
                        "method_not_allowed",
                        "HTTP-метод не поддерживается."
                    ),

                StatusCodes.Status409Conflict =>
                    (
                        "conflict",
                        "Запрос конфликтует с текущим состоянием ресурса."
                    ),

                StatusCodes.Status415UnsupportedMediaType =>
                    (
                        "unsupported_media_type",
                        "Неподдерживаемый тип содержимого."
                    ),

                StatusCodes.Status429TooManyRequests =>
                    (
                        "too_many_requests",
                        "Слишком много запросов."
                    ),

                StatusCodes.Status500InternalServerError =>
                    (
                        "internal_error",
                        "Внутренняя ошибка сервера."
                    ),

                _ =>
                    (
                        "http_error",
                        "HTTP-запрос завершился с ошибкой."
                    )
            };

        return Create(
            httpContext,
            status,
            code,
            message);
    }
}