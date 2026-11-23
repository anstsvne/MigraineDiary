using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Medications;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Data;
using MigraineDiary.Api.Enums;
using MigraineDiary.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class AuthApiTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    public AuthApiTests(
        ApiTestFixture fixture)
    {
        _fixture = fixture;
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
            await LoginAsync(
                email,
                "Password123");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await ReadJsonAsync<LoginResponse>(
                response);

        Assert.NotNull(result);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.Equal(
            email,
            result.Email);

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
            await LoginAsync(
                email,
                "WrongPassword123");

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
        var session =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/auth/me",
                session.Token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await ReadJsonAsync<MeResponse>(
                response);

        Assert.NotNull(user);

        Assert.Equal(
            session.Email,
            user.Email);

        Assert.Equal(
            "Test User",
            user.Name);
    }

    [Fact]
    public async Task UpdateMe_ShouldUpdateNameAndEmail()
    {
        var session =
            await CreateUserAndLoginAsync();

        var newEmail =
            CreateEmail();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                "/api/auth/me",
                session.Token,
                new UpdateProfileRequest
                {
                    Name = "Updated User",
                    Email = newEmail
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await ReadJsonAsync<MeResponse>(
                response);

        Assert.NotNull(updated);

        Assert.Equal(
            session.UserId,
            updated.Id);

        Assert.Equal(
            "Updated User",
            updated.Name);

        Assert.Equal(
            newEmail,
            updated.Email);

        var meResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/auth/me",
                session.Token);

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var currentUser =
            await ReadJsonAsync<MeResponse>(
                meResponse);

        Assert.NotNull(currentUser);

        Assert.Equal(
            "Updated User",
            currentUser.Name);

        Assert.Equal(
            newEmail,
            currentUser.Email);

        var oldEmailLogin =
            await LoginAsync(
                session.Email,
                session.Password);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldEmailLogin.StatusCode);

        var newEmailLogin =
            await LoginAsync(
                newEmail,
                session.Password);

        Assert.Equal(
            HttpStatusCode.OK,
            newEmailLogin.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithExistingEmail_ShouldReturnConflict()
    {
        var firstUser =
            await CreateUserAndLoginAsync();

        var secondUser =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                "/api/auth/me",
                secondUser.Token,
                new UpdateProfileRequest
                {
                    Name = "Second User",
                    Email = firstUser.Email
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var session =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                "/api/auth/me",
                session.Token,
                new UpdateProfileRequest
                {
                    Name = "Updated User",
                    Email = "not-an-email"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReplacePassword()
    {
        var session =
            await CreateUserAndLoginAsync();

        const string newPassword =
            "NewPassword456";

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/auth/change-password",
                session.Token,
                new ChangePasswordRequest
                {
                    CurrentPassword =
                        session.Password,

                    NewPassword =
                        newPassword,

                    ConfirmNewPassword =
                        newPassword
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var oldPasswordLogin =
            await LoginAsync(
                session.Email,
                session.Password);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldPasswordLogin.StatusCode);

        var newPasswordLogin =
            await LoginAsync(
                session.Email,
                newPassword);

        Assert.Equal(
            HttpStatusCode.OK,
            newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturnBadRequest()
    {
        var session =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/auth/change-password",
                session.Token,
                new ChangePasswordRequest
                {
                    CurrentPassword =
                        "WrongPassword123",

                    NewPassword =
                        "NewPassword456",

                    ConfirmNewPassword =
                        "NewPassword456"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var loginResponse =
            await LoginAsync(
                session.Email,
                session.Password);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithMismatchedConfirmation_ShouldReturnBadRequest()
    {
        var session =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/auth/change-password",
                session.Token,
                new ChangePasswordRequest
                {
                    CurrentPassword =
                        session.Password,

                    NewPassword =
                        "NewPassword456",

                    ConfirmNewPassword =
                        "DifferentPassword456"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_WithWrongPassword_ShouldReturnBadRequest()
    {
        var session =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                "/api/auth/me",
                session.Token,
                new DeleteAccountRequest
                {
                    Password =
                        "WrongPassword123"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var loginResponse =
            await LoginAsync(
                session.Email,
                session.Password);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_ShouldDeleteAccountAndRelatedData()
    {
        var session =
            await CreateUserAndLoginAsync();

        var medicationResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/medications",
                session.Token,
                new CreateMedicationRequest
                {
                    Name = "Delete account medication"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            medicationResponse.StatusCode);

        var medication =
            await ReadJsonAsync<MedicationResponse>(
                medicationResponse);

        Assert.NotNull(medication);

        var attackResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/migraine-attacks",
                session.Token,
                new CreateMigraineAttackRequest
                {
                    StartAt =
                        new DateTimeOffset(
                            2026, 11, 21,
                            8, 0, 0,
                            TimeSpan.Zero),

                    EndAt =
                        new DateTimeOffset(
                            2026, 11, 21,
                            12, 0, 0,
                            TimeSpan.Zero),

                    Intensity = 7,

                    PainLocation =
                        PainLocation.LeftSide,

                    Notes =
                        "Account deletion test",

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
                                    2026, 11, 21,
                                    9, 0, 0,
                                    TimeSpan.Zero)
                        }
                    ]
                });

        Assert.Equal(
            HttpStatusCode.Created,
            attackResponse.StatusCode);

        var vitalResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/vital-measurements",
                session.Token,
                new CreateVitalMeasurementRequest
                {
                    SystolicPressure = 120,
                    DiastolicPressure = 80,
                    Pulse = 70,

                    MeasuredAt =
                        new DateTimeOffset(
                            2026, 11, 21,
                            10, 0, 0,
                            TimeSpan.Zero),

                    Notes =
                        "Account deletion test"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            vitalResponse.StatusCode);

        using (var scope =
               _fixture.Factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var linkExists =
                await dbContext.AttackMedications
                    .AnyAsync(
                        x =>
                            x.MedicationId ==
                            medication.Id);

            Assert.True(linkExists);
        }

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                "/api/auth/me",
                session.Token,
                new DeleteAccountRequest
                {
                    Password =
                        session.Password
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        using (var scope =
               _fixture.Factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            Assert.False(
                await dbContext.Users
                    .AnyAsync(
                        x =>
                            x.Id ==
                            session.UserId));

            Assert.False(
                await dbContext.MigraineAttacks
                    .AnyAsync(
                        x =>
                            x.UserId ==
                            session.UserId));

            Assert.False(
                await dbContext.Medications
                    .AnyAsync(
                        x =>
                            x.UserId ==
                            session.UserId));

            Assert.False(
                await dbContext.VitalMeasurements
                    .AnyAsync(
                        x =>
                            x.UserId ==
                            session.UserId));

            Assert.False(
                await dbContext.AttackMedications
                    .AnyAsync(
                        x =>
                            x.MedicationId ==
                            medication.Id));
        }

        var loginResponse =
            await LoginAsync(
                session.Email,
                session.Password);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);

        var meResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/auth/me",
                session.Token);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            meResponse.StatusCode);
    }

    [Fact]
    public async Task DeletedUserToken_ShouldNotAllowCreatingAttack()
    {
        var session =
            await CreateUserAndLoginAsync();

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                "/api/auth/me",
                session.Token,
                new DeleteAccountRequest
                {
                    Password =
                        session.Password
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var attackResponse =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/migraine-attacks",
                session.Token,
                new CreateMigraineAttackRequest
                {
                    StartAt =
                        new DateTimeOffset(
                            2026, 11, 23,
                            10, 0, 0,
                            TimeSpan.Zero),

                    EndAt =
                        new DateTimeOffset(
                            2026, 11, 23,
                            12, 0, 0,
                            TimeSpan.Zero),

                    Intensity = 6,

                    PainLocation =
                        PainLocation.LeftSide,

                    Notes =
                        "Deleted user stale token test"
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            attackResponse.StatusCode);
    }

    private async Task<UserSession>
        CreateUserAndLoginAsync(
            string password = "Password123")
    {
        var email =
            CreateEmail();

        await RegisterAsync(
            email,
            password);

        var loginResponse =
            await LoginAsync(
                email,
                password);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await ReadJsonAsync<LoginResponse>(
                loginResponse);

        Assert.NotNull(login);

        return new UserSession(
            email,
            password,
            login.UserId,
            login.AccessToken);
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

    private async Task<HttpResponseMessage>
        LoginAsync(
            string email,
            string password)
    {
        return await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = password
            });
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

    private static string CreateEmail()
    {
        return
            $"test-{Guid.NewGuid():N}@example.com";
    }

    private sealed record UserSession(
        string Email,
        string Password,
        Guid UserId,
        string Token);
}
