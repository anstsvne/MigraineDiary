using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class DataExportApiTests
{
    private readonly HttpClient _client;

    public DataExportApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Export_WithoutToken_ShouldReturnUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/data-export");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Export_ShouldContainOnlyCurrentUsersData()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        await CreateMeasurementAsync(
            firstToken,
            "FIRST_USER_UNIQUE_NOTE");

        await CreateMeasurementAsync(
            secondToken,
            "SECOND_USER_UNIQUE_NOTE");

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/data-export",
                firstToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/zip",
            response.Content.Headers.ContentType?.MediaType);

        await using var stream =
            await response.Content.ReadAsStreamAsync();

        using var archive =
            new ZipArchive(
                stream,
                ZipArchiveMode.Read);

        var attacksEntry =
            archive.GetEntry(
                "migraine-attacks.csv");

        var vitalsEntry =
            archive.GetEntry(
                "vital-measurements.csv");

        Assert.NotNull(attacksEntry);
        Assert.NotNull(vitalsEntry);

        using var reader =
            new StreamReader(
                vitalsEntry.Open());

        var content =
            await reader.ReadToEndAsync();

        Assert.Contains(
            "FIRST_USER_UNIQUE_NOTE",
            content);

        Assert.DoesNotContain(
            "SECOND_USER_UNIQUE_NOTE",
            content);
    }

    [Fact]
    public async Task Export_WithInvalidPeriod_ShouldReturnBadRequest()
    {
        var token =
            await CreateUserAndLoginAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/data-export" +
                "?from=2026-12-31T00:00:00Z" +
                "&to=2026-12-01T00:00:00Z",
                token);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task CreateMeasurementAsync(
        string token,
        string notes)
    {
        var request =
            new CreateVitalMeasurementRequest
            {
                SystolicPressure = 120,
                DiastolicPressure = 80,
                Pulse = 70,
                MeasuredAt =
                    new DateTimeOffset(
                        2026, 11, 12,
                        12, 0, 0,
                        TimeSpan.Zero),
                Notes = notes
            };

        using var message =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/vital-measurements",
                token);

        message.Content =
            JsonContent.Create(request);

        var response =
            await _client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            $"export-{Guid.NewGuid():N}@example.com";

        const string password =
            "Password123";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name = "Export Test User",
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