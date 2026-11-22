using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class ErrorHandlingApiTests
{
    private readonly HttpClient _client;

    public ErrorHandlingApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task ValidationError_ShouldReturnUnifiedResponse()
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name = string.Empty,
                    Email = "invalid-email",
                    Password = "123",
                    ConfirmPassword = "456"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            400,
            error.Status);

        Assert.Equal(
            "validation_error",
            error.Code);

        Assert.Equal(
            "Запрос содержит некорректные данные.",
            error.Message);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));

        Assert.NotNull(
            error.Errors);

        Assert.Contains(
            "Email",
            error.Errors.Keys);

        Assert.Contains(
            "Password",
            error.Errors.Keys);
    }

    [Fact]
    public async Task Unauthorized_ShouldReturnUnifiedResponse()
    {
        var response =
            await _client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            401,
            error.Status);

        Assert.Equal(
            "unauthorized",
            error.Code);

        Assert.Equal(
            "Требуется авторизация.",
            error.Message);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));

        Assert.Null(
            error.Errors);
    }

    [Fact]
    public async Task InvalidCredentials_ShouldReturnUnifiedResponse()
    {
        var email =
            CreateEmail();

        await RegisterAsync(
            email,
            "Password123");

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "WrongPassword123"
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            401,
            error.Status);

        Assert.Equal(
            "invalid_credentials",
            error.Code);

        Assert.Equal(
            "Неверный email или пароль.",
            error.Message);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));
    }

    [Fact]
    public async Task NotFound_ShouldReturnUnifiedResponse()
    {
        var response =
            await _client.GetAsync(
                "/api/route-that-does-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            404,
            error.Status);

        Assert.Equal(
            "not_found",
            error.Code);

        Assert.Equal(
            "Ресурс не найден.",
            error.Message);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));
    }

    [Fact]
    public async Task Conflict_ShouldReturnUnifiedResponse()
    {
        var email =
            CreateEmail();

        var request =
            new RegisterRequest
            {
                Name = "Conflict Test User",
                Email = email,
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

        var firstResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        var error =
            await secondResponse.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            409,
            error.Status);

        Assert.Equal(
            "conflict",
            error.Code);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.Message));

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));
    }

    [Fact]
    public async Task BusinessValidation_ShouldReturnUnifiedResponse()
    {
        var token =
            await CreateUserAndLoginAsync();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/data-export" +
                "?from=2026-12-31T00:00:00Z" +
                "&to=2026-12-01T00:00:00Z");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response =
            await _client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            400,
            error.Status);

        Assert.Equal(
            "invalid_request",
            error.Code);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.Message));

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));

        Assert.Null(
            error.Errors);
    }

    [Fact]
    public async Task MethodNotAllowed_ShouldReturnUnifiedResponse()
    {
        var token =
            await CreateUserAndLoginAsync();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/symptoms");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response =
            await _client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.MethodNotAllowed,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            405,
            error.Status);

        Assert.Equal(
            "method_not_allowed",
            error.Code);

        Assert.Equal(
            "HTTP-метод не поддерживается.",
            error.Message);

        Assert.False(
            string.IsNullOrWhiteSpace(
                error.TraceId));
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            CreateEmail();

        const string password =
            "Password123";

        await RegisterAsync(
            email,
            password);

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        return login.AccessToken;
    }

    private async Task RegisterAsync(
        string email,
        string password)
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name = "Error Test User",
                    Email = email,
                    Password = password,
                    ConfirmPassword = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private static string CreateEmail()
    {
        return
            $"error-{Guid.NewGuid():N}@example.com";
    }
}