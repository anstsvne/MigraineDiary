using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Contracts.MigraineAttacks;
using MigraineDiary.Api.Enums;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class MigraineAttackApiTests
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

    public MigraineAttackApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Create_ShouldCreateAttack()
    {
        var token =
            await CreateUserAndLoginAsync();

        var request =
            CreateAttackRequest(
                startAt:
                    new DateTimeOffset(
                        2026, 11, 1,
                        10, 0, 0,
                        TimeSpan.Zero),

                endAt:
                    new DateTimeOffset(
                        2026, 11, 1,
                        14, 0, 0,
                        TimeSpan.Zero),

                intensity: 7,

                painLocation:
                    PainLocation.LeftSide,

                notes:
                    "Integration test attack");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/migraine-attacks",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var attack =
            await ReadJsonAsync<MigraineAttackResponse>(
                response);

        Assert.NotNull(attack);

        Assert.NotEqual(
            Guid.Empty,
            attack.Id);

        Assert.Equal(
            7,
            attack.Intensity);

        Assert.Equal(
            PainLocation.LeftSide,
            attack.PainLocation);

        Assert.Equal(
            "Integration test attack",
            attack.Notes);

        Assert.Equal(
            240,
            attack.DurationMinutes);
    }

    [Fact]
    public async Task GetById_ShouldReturnCreatedAttack()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                token,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 2,
                        8, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 2,
                        10, 0, 0,
                        TimeSpan.Zero),

                    5,
                    PainLocation.Temples,
                    "Get by id test"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/migraine-attacks/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var attack =
            await ReadJsonAsync<MigraineAttackResponse>(
                response);

        Assert.NotNull(attack);

        Assert.Equal(
            created.Id,
            attack.Id);

        Assert.Equal(
            5,
            attack.Intensity);

        Assert.Equal(
            PainLocation.Temples,
            attack.PainLocation);
    }

    [Fact]
    public async Task Update_ShouldChangeAttack()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                token,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 3,
                        9, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 3,
                        11, 0, 0,
                        TimeSpan.Zero),

                    4,
                    PainLocation.RightSide,
                    "Before update"));

        var updateRequest =
            new UpdateMigraineAttackRequest
            {
                StartAt =
                    new DateTimeOffset(
                        2026, 11, 3,
                        9, 30, 0,
                        TimeSpan.Zero),

                EndAt =
                    new DateTimeOffset(
                        2026, 11, 3,
                        13, 30, 0,
                        TimeSpan.Zero),

                Intensity = 8,

                PainLocation =
                    PainLocation.BothSides,

                PainLocationDescription =
                    "Both sides",

                Notes =
                    "After update"
            };

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/migraine-attacks/{created.Id}",
                token,
                updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await ReadJsonAsync<MigraineAttackResponse>(
                response);

        Assert.NotNull(updated);

        Assert.Equal(
            created.Id,
            updated.Id);

        Assert.Equal(
            8,
            updated.Intensity);

        Assert.Equal(
            PainLocation.BothSides,
            updated.PainLocation);

        Assert.Equal(
            "After update",
            updated.Notes);

        Assert.Equal(
            240,
            updated.DurationMinutes);
    }

    [Fact]
    public async Task Delete_ShouldRemoveAttack()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                token,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 4,
                        10, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 4,
                        12, 0, 0,
                        TimeSpan.Zero),

                    5,
                    PainLocation.Forehead,
                    "Delete test"));

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/migraine-attacks/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/migraine-attacks/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherUsersAttack_ShouldReturnNotFound()
    {
        var firstUserToken =
            await CreateUserAndLoginAsync();

        var secondUserToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                firstUserToken,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 5,
                        10, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 5,
                        12, 0, 0,
                        TimeSpan.Zero),

                    6,
                    PainLocation.LeftSide,
                    "Private attack"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/migraine-attacks/{created.Id}",
                secondUserToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherUsersAttack_ShouldReturnNotFound()
    {
        var ownerToken =
            await CreateUserAndLoginAsync();

        var anotherUserToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                ownerToken,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 20,
                        10, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 20,
                        12, 0, 0,
                        TimeSpan.Zero),

                    6,
                    PainLocation.LeftSide,
                    "Owner attack"));

        var updateRequest =
            new UpdateMigraineAttackRequest
            {
                StartAt =
                    new DateTimeOffset(
                        2026, 11, 20,
                        11, 0, 0,
                        TimeSpan.Zero),

                EndAt =
                    new DateTimeOffset(
                        2026, 11, 20,
                        14, 0, 0,
                        TimeSpan.Zero),

                Intensity = 9,

                PainLocation =
                    PainLocation.RightSide,

                Notes =
                    "Unauthorized update"
            };

        var updateResponse =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/migraine-attacks/{created.Id}",
                anotherUserToken,
                updateRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        var ownerResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/migraine-attacks/{created.Id}",
                ownerToken);

        Assert.Equal(
            HttpStatusCode.OK,
            ownerResponse.StatusCode);

        var unchanged =
            await ReadJsonAsync<MigraineAttackResponse>(
                ownerResponse);

        Assert.NotNull(unchanged);

        Assert.Equal(
            6,
            unchanged.Intensity);

        Assert.Equal(
            PainLocation.LeftSide,
            unchanged.PainLocation);

        Assert.Equal(
            "Owner attack",
            unchanged.Notes);
    }

    [Fact]
    public async Task Delete_ForAnotherUsersAttack_ShouldReturnNotFound()
    {
        var ownerToken =
            await CreateUserAndLoginAsync();

        var anotherUserToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateAttackAsync(
                ownerToken,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 21,
                        10, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 21,
                        12, 0, 0,
                        TimeSpan.Zero),

                    7,
                    PainLocation.Temples,
                    "Protected attack"));

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/migraine-attacks/{created.Id}",
                anotherUserToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            deleteResponse.StatusCode);

        var ownerResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/migraine-attacks/{created.Id}",
                ownerToken);

        Assert.Equal(
            HttpStatusCode.OK,
            ownerResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyCurrentUsersAttacks()
    {
        var firstUserToken =
            await CreateUserAndLoginAsync();

        var secondUserToken =
            await CreateUserAndLoginAsync();

        var firstUserAttack =
            await CreateAttackAsync(
                firstUserToken,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 6,
                        10, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 6,
                        12, 0, 0,
                        TimeSpan.Zero),

                    6,
                    PainLocation.LeftSide,
                    "First user"));

        await CreateAttackAsync(
            secondUserToken,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 6,
                    13, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 6,
                    15, 0, 0,
                    TimeSpan.Zero),

                9,
                PainLocation.RightSide,
                "Second user"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks",
                firstUserToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await ReadJsonAsync<
                PagedResponse<MigraineAttackResponse>>(
                response);

        Assert.NotNull(page);

        Assert.Equal(
            1,
            page.Page);

        Assert.Equal(
            20,
            page.PageSize);

        Assert.Equal(
            1,
            page.TotalCount);

        Assert.Equal(
            1,
            page.TotalPages);

        Assert.Single(
            page.Items);

        Assert.Equal(
            firstUserAttack.Id,
            page.Items[0].Id);

        Assert.Equal(
            "First user",
            page.Items[0].Notes);
    }

    [Fact]
    public async Task FilterByIntensity_ShouldReturnMatchingAttacks()
    {
        var token =
            await CreateUserAndLoginAsync();

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 7,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 7,
                    10, 0, 0,
                    TimeSpan.Zero),

                3,
                PainLocation.Forehead,
                "Low intensity"));

        var highIntensityAttack =
            await CreateAttackAsync(
                token,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 7,
                        12, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 7,
                        16, 0, 0,
                        TimeSpan.Zero),

                    8,
                    PainLocation.Temples,
                    "High intensity"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks?minIntensity=7",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await ReadJsonAsync<
                PagedResponse<MigraineAttackResponse>>(
                response);

        Assert.NotNull(page);

        Assert.Equal(
            1,
            page.TotalCount);

        Assert.Equal(
            1,
            page.TotalPages);

        Assert.Single(
            page.Items);

        Assert.Equal(
            highIntensityAttack.Id,
            page.Items[0].Id);

        Assert.Equal(
            8,
            page.Items[0].Intensity);
    }

    [Fact]
    public async Task Pagination_ShouldReturnRequestedPage()
    {
        var token =
            await CreateUserAndLoginAsync();

        var oldest =
            await CreateAttackAsync(
                token,
                CreateAttackRequest(
                    new DateTimeOffset(
                        2026, 11, 10,
                        8, 0, 0,
                        TimeSpan.Zero),

                    new DateTimeOffset(
                        2026, 11, 10,
                        10, 0, 0,
                        TimeSpan.Zero),

                    4,
                    PainLocation.LeftSide,
                    "Oldest"));

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 11,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 11,
                    10, 0, 0,
                    TimeSpan.Zero),

                5,
                PainLocation.RightSide,
                "Middle"));

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 12,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 12,
                    10, 0, 0,
                    TimeSpan.Zero),

                6,
                PainLocation.Forehead,
                "Newest"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks?page=2&pageSize=2",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await ReadJsonAsync<
                PagedResponse<MigraineAttackResponse>>(
                response);

        Assert.NotNull(page);

        Assert.Equal(
            2,
            page.Page);

        Assert.Equal(
            2,
            page.PageSize);

        Assert.Equal(
            3,
            page.TotalCount);

        Assert.Equal(
            2,
            page.TotalPages);

        Assert.Single(
            page.Items);

        Assert.Equal(
            oldest.Id,
            page.Items[0].Id);
    }

    [Fact]
    public async Task Pagination_WithInvalidPage_ShouldReturnBadRequest()
    {
        var token =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks?page=0&pageSize=20",
                token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Pagination_BeyondLastPage_ShouldReturnEmptyItems()
    {
        var token =
            await CreateUserAndLoginAsync();

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 13,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 13,
                    10, 0, 0,
                    TimeSpan.Zero),

                5,
                PainLocation.Temples,
                "Only attack"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks?page=5&pageSize=2",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await ReadJsonAsync<
                PagedResponse<MigraineAttackResponse>>(
                response);

        Assert.NotNull(page);

        Assert.Equal(
            5,
            page.Page);

        Assert.Equal(
            2,
            page.PageSize);

        Assert.Equal(
            1,
            page.TotalCount);

        Assert.Equal(
            1,
            page.TotalPages);

        Assert.Empty(
            page.Items);
    }

    [Fact]
    public async Task Statistics_ShouldCalculateCurrentUsersAttacks()
    {
        var token =
            await CreateUserAndLoginAsync();

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 8,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 8,
                    10, 0, 0,
                    TimeSpan.Zero),

                4,
                PainLocation.LeftSide,
                "Statistics 1"));

        await CreateAttackAsync(
            token,
            CreateAttackRequest(
                new DateTimeOffset(
                    2026, 11, 9,
                    8, 0, 0,
                    TimeSpan.Zero),

                new DateTimeOffset(
                    2026, 11, 9,
                    12, 0, 0,
                    TimeSpan.Zero),

                8,
                PainLocation.RightSide,
                "Statistics 2"));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks/statistics",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var statistics =
            await ReadJsonAsync<
                MigraineAttackStatisticsResponse>(
                response);

        Assert.NotNull(statistics);

        Assert.Equal(
            2,
            statistics.AttackCount);

        Assert.Equal(
            6,
            statistics.AverageIntensity);

        Assert.Equal(
            180,
            statistics.AverageDurationMinutes);
    }

    [Fact]
    public async Task Statistics_WithInvalidPeriod_ShouldReturnBadRequest()
    {
        var token =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/migraine-attacks/statistics" +
                "?from=2026-11-30T00:00:00Z" +
                "&to=2026-11-01T00:00:00Z",
                token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<MigraineAttackResponse>
        CreateAttackAsync(
            string token,
            CreateMigraineAttackRequest request)
    {
        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/migraine-attacks",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await ReadJsonAsync<MigraineAttackResponse>(
                response);

        Assert.NotNull(result);

        return result;
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            $"attack-{Guid.NewGuid():N}@example.com";

        const string password =
            "Password123";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name =
                        "Attack Test User",

                    Email =
                        email,

                    Password =
                        password,

                    ConfirmPassword =
                        password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email =
                        email,

                    Password =
                        password
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

    private static CreateMigraineAttackRequest
        CreateAttackRequest(
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            int intensity,
            PainLocation painLocation,
            string notes)
    {
        return new CreateMigraineAttackRequest
        {
            StartAt =
                startAt,

            EndAt =
                endAt,

            Intensity =
                intensity,

            PainLocation =
                painLocation,

            PainLocationDescription =
                "Integration test location",

            Notes =
                notes
        };
    }
}
