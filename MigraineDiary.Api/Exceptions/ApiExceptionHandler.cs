using Microsoft.AspNetCore.Diagnostics;

namespace MigraineDiary.Api.Exceptions;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, code, message) =
            exception switch
            {
                ArgumentException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "invalid_request",
                        exception.Message
                    ),

                ConflictException =>
                    (
                        StatusCodes.Status409Conflict,
                        "conflict",
                        exception.Message
                    ),

                _ =>
                    (
                        StatusCodes.Status500InternalServerError,
                        "internal_error",
                        "¬нутренн€€ ошибка сервера."
                    )
            };

        if (statusCode ==
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request {Method} {Path} failed with status code {StatusCode}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode);
        }

        httpContext.Response.StatusCode =
            statusCode;

        var response =
            ApiErrorResponseFactory.Create(
                httpContext,
                statusCode,
                code,
                message);

        await httpContext.Response.WriteAsJsonAsync(
            response,
            cancellationToken);

        return true;
    }
}