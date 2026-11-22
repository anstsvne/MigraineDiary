using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Enums;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class MedicationApiTests
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

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
            await ReadJsonAsync<MedicationResponse>(
                response);

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

        await CreateMedicationAsync(
            firstToken,
            "First user medication");

        await CreateMedicationAsync(
            secondToken,
            "Second user medication");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/medications",
                firstToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var medications =
            await ReadJsonAsync<
                List<MedicationResponse>>(
                response);

        Assert.NotNull(medications);
        Assert.Single(medications);

        Assert.Equal(
            "First user medication",
            medications[0].Name);
    }

    [Fact]
    public async Task GetById_ShouldReturnMedication()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMedicationAsync(
                token,
                "Paracetamol");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var medication =
            await ReadJsonAsync<MedicationResponse>(
                response);

        Assert.NotNull(medication);

        Assert.Equal(
            created.Id,
            medication.Id);

        Assert.Equal(
            "Paracetamol",
            medication.Name);
    }

    [Fact]
    public async Task GetById_ForAnotherUser_ShouldReturnNotFound()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        var medication =
            await CreateMedicationAsync(
                firstToken,
                "Private medication");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{medication.Id}",
                secondToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldRenameMedication()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMedicationAsync(
                token,
                "Old medication name");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/medications/{created.Id}",
                token,
                new UpdateMedicationRequest
                {
                    Name = "New medication name"
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await ReadJsonAsync<MedicationResponse>(
                response);

        Assert.NotNull(updated);

        Assert.Equal(
            created.Id,
            updated.Id);

        Assert.Equal(
            "New medication name",
            updated.Name);

        var getResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var stored =
            await ReadJsonAsync<MedicationResponse>(
                getResponse);

        Assert.NotNull(stored);

        Assert.Equal(
            "New medication name",
            stored.Name);
    }

    [Fact]
    public async Task Update_ToDuplicateName_ShouldReturnConflict()
    {
        var token =
            await CreateUserAndLoginAsync();

        await CreateMedicationAsync(
            token,
            "First medication");

        var second =
            await CreateMedicationAsync(
                token,
                "Second medication");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/medications/{second.Id}",
                token,
                new UpdateMedicationRequest
                {
                    Name = "First medication"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherUser_ShouldReturnNotFound()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        var medication =
            await CreateMedicationAsync(
                firstToken,
                "First user's medication");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/medications/{medication.Id}",
                secondToken,
                new UpdateMedicationRequest
                {
                    Name = "Unauthorized update"
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldRemoveMedication()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMedicationAsync(
                token,
                "Medication to delete");

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/medications/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ForAnotherUser_ShouldReturnNotFound()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        var medication =
            await CreateMedicationAsync(
                firstToken,
                "Protected medication");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/medications/{medication.Id}",
                secondToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var ownerResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{medication.Id}",
                firstToken);

        Assert.Equal(
            HttpStatusCode.OK,
            ownerResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UsedMedication_ShouldReturnConflict()
    {
        var token =
            await CreateUserAndLoginAsync();

        var medication =
            await CreateMedicationAsync(
                token,
                "Medication used in attack");

        var attackResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/migraine-attacks",
                token,
                new CreateMigraineAttackRequest
                {
                    StartAt =
                        new DateTimeOffset(
                            2026, 11, 20,
                            8, 0, 0,
                            TimeSpan.Zero),

                    EndAt =
                        new DateTimeOffset(
                            2026, 11, 20,
                            12, 0, 0,
                            TimeSpan.Zero),

                    Intensity = 7,

                    PainLocation =
                        PainLocation.LeftSide,

                    Notes =
                        "Medication delete protection test",

                    Medications =
                    [
                        new AttackMedicationRequest
                        {
                            MedicationId =
                                medication.Id,

                            Dosage =
                                "400 mg",

                            TakenAt =
                                new DateTimeOffset(
                                    2026, 11, 20,
                                    9, 0, 0,
                                    TimeSpan.Zero)
                        }
                    ]
                });

        Assert.Equal(
            HttpStatusCode.Created,
            attackResponse.StatusCode);

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/medications/{medication.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.Conflict,
            deleteResponse.StatusCode);

        var getResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/medications/{medication.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);
    }

    private async Task<MedicationResponse>
        CreateMedicationAsync(
            string token,
            string name)
    {
        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/medications",
                token,
                new CreateMedicationRequest
                {
                    Name = name
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var medication =
            await ReadJsonAsync<MedicationResponse>(
                response);

        Assert.NotNull(medication);

        return medication;
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
                JsonContent.Create(
                    body,
                    options: JsonOptions);
        }

        return await _client.SendAsync(
            request);
    }

    private static async Task<T?>
        ReadJsonAsync<T>(
            HttpResponseMessage response)
    {
        return await response.Content
            .ReadFromJsonAsync<T>(
                JsonOptions);
    }
}
