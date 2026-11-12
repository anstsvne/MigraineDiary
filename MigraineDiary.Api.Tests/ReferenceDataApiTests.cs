using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Symptoms;
using MigraineDiary.Api.Contracts.Triggers;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class ReferenceDataApiTests
{
    private readonly HttpClient _client;

    public ReferenceDataApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Symptoms_ShouldReturnSeededSymptoms()
    {
        var token = await CreateUserAndLoginAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/symptoms",
                token);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var symptoms =
            await response.Content
                .ReadFromJsonAsync<
                    List<SymptomResponse>>();

        Assert.NotNull(symptoms);

        Assert.Equal(
            7,
            symptoms.Count);

        Assert.Contains(
            symptoms,
            x =>
                x.Id ==
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111") &&
                x.Name == "Nausea");
    }

    [Fact]
    public async Task Triggers_ShouldReturnSeededTriggers()
    {
        var token = await CreateUserAndLoginAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/triggers",
                token);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var triggers =
            await response.Content
                .ReadFromJsonAsync<
                    List<TriggerResponse>>();

        Assert.NotNull(triggers);

        Assert.Equal(
            9,
            triggers.Count);

        Assert.Contains(
            triggers,
            x =>
                x.Id ==
                Guid.Parse(
                    "81111111-1111-1111-1111-111111111111") &&
                x.Name == "Stress");
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            $"reference-{Guid.NewGuid():N}@example.com";

        const string password =
            "Password123";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name = "Reference Test User",
                    Email = email,
                    Password = password,
                    ConfirmPassword = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

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

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string uri,
            string token)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}