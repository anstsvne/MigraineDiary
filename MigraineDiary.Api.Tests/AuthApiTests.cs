using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class AuthApiTests
{
    private readonly HttpClient _client;

    public AuthApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Register_ShouldCreateUser()
    {
        var email = CreateEmail();

        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = email,
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<RegisterResponse>();

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal("Test User", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnConflict()
    {
        var email = CreateEmail();

        var request = new RegisterRequest
        {
            Name = "Test User",
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
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var email = CreateEmail();

        await RegisterAsync(
            email,
            "Password123");

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "Password123"
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.Equal(email, result.Email);
        Assert.True(
            result.ExpiresAt >
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        var email = CreateEmail();

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
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturnUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturnCurrentUser()
    {
        var email = CreateEmail();

        await RegisterAsync(
            email,
            "Password123");

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = "Password123"
                });

        loginResponse.EnsureSuccessStatusCode();

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal("Test User", user.Name);
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
                    Name = "Test User",
                    Email = email,
                    Password = password,
                    ConfirmPassword = password
                });

        response.EnsureSuccessStatusCode();
    }

    private static string CreateEmail()
    {
        return
            $"test-{Guid.NewGuid():N}@example.com";
    }
}