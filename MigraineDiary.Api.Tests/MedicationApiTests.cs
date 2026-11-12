using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class MedicationApiTests
{
    private readonly HttpClient _client;

    public MedicationApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Create_ShouldCreateMedication()
    {
        var token = await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/medications",
                token,
                new CreateMedicationRequest
                {
                    Name = "Ibuprofen"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var medication =
            await response.Content
                .ReadFromJsonAsync<MedicationResponse>();

        Assert.NotNull(medication);
        Assert.NotEqual(
            Guid.Empty,
            medication.Id);
        Assert.Equal(
            "Ibuprofen",
            medication.Name);
    }

    [Fact]
    public async Task Create_DuplicateMedication_ShouldReturnConflict()
    {
        var token = await CreateUserAndLoginAsync();

        var request =
            new CreateMedicationRequest
            {
                Name = "Sumatriptan"
            };

        var firstResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/medications",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/medications",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyCurrentUsersMedications()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/medications",
            firstToken,
            new CreateMedicationRequest
            {
                Name = "First user medication"
            });

        await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/medications",
            secondToken,
            new CreateMedicationRequest
            {
                Name = "Second user medication"
            });

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/medications",
                firstToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var medications =
            await response.Content
                .ReadFromJsonAsync<
                    List<MedicationResponse>>();

        Assert.NotNull(medications);
        Assert.Single(medications);

        Assert.Equal(
            "First user medication",
            medications[0].Name);
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            $"medication-{Guid.NewGuid():N}@example.com";

        const string password =
            "Password123";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name = "Medication Test User",
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

    private async Task<HttpResponseMessage>
        SendAuthorizedAsync(
            HttpMethod method,
            string uri,
            string token,
            object? body = null)
    {
        using var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        if (body is not null)
        {
            request.Content =
                JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }
}