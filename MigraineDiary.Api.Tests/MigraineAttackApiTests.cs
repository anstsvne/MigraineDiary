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

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CalendarPeriod_ShouldRoundTripOptionalTimes(
        bool hasStartTime,
        bool hasEndTime)
    {
        var token = await CreateUserAndLoginAsync();
        var startDate = new DateOnly(2026, 11, 10);
        var endDate = new DateOnly(2026, 11, 11);
        DateTimeOffset? startAt = hasStartTime
            ? new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.FromHours(7))
            : null;
        DateTimeOffset? endAt = hasEndTime
            ? new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.FromHours(7))
            : null;

        var created = await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            StartAt = startAt,
            EndAt = endAt,
            Intensity = 6,
            PainLocation = PainLocation.LeftSide
        });

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/migraine-attacks/{created.Id}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loaded = await ReadJsonAsync<MigraineAttackResponse>(response);
        Assert.NotNull(loaded);

        foreach (var attack in new[] { created, loaded! })
        {
            Assert.Equal(startDate, attack.StartDate);
            Assert.Equal(endDate, attack.EndDate);
            Assert.Equal(2, attack.DurationDays);
            Assert.Equal(startAt?.ToUniversalTime(), attack.StartAt);
            Assert.Equal(endAt?.ToUniversalTime(), attack.EndAt);
            if (hasStartTime && hasEndTime)
                Assert.Equal(1560d, attack.DurationMinutes);
            else
                Assert.Null(attack.DurationMinutes);
        }
    }

    [Fact]
    public async Task LegacyCreate_ShouldDeriveCalendarDatesFromUtc()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(
            2026, 11, 10, 1, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddHours(8);

        var created = await CreateAttackAsync(token,
            CreateAttackRequest(start, end, 6, PainLocation.LeftSide, "Legacy"));

        Assert.Equal(new DateOnly(2026, 11, 9), created.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 10), created.EndDate);
        Assert.Equal(start.ToUniversalTime(), created.StartAt);
        Assert.Equal(end.ToUniversalTime(), created.EndAt);
        Assert.Equal(480d, created.DurationMinutes);
        Assert.Equal(2, created.DurationDays);
    }

    [Fact]
    public async Task CalendarCreate_ShouldKeepExplicitDatesAcrossUtcMidnight()
    {
        var token = await CreateUserAndLoginAsync();
        var date = new DateOnly(2026, 11, 10);
        var start = new DateTimeOffset(
            2026, 11, 10, 1, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddHours(2);

        var created = await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = date,
            EndDate = date,
            StartAt = start,
            EndAt = end,
            Intensity = 5,
            PainLocation = PainLocation.LeftSide
        });

        Assert.Equal(date, created.StartDate);
        Assert.Equal(date, created.EndDate);
        Assert.Equal(1, created.DurationDays);
        Assert.Equal(start.ToUniversalTime(), created.StartAt);
        Assert.Equal(120d, created.DurationMinutes);
    }

    [Fact]
    public async Task CalendarUpdate_ShouldClearAndRestoreExactTimes()
    {
        var token = await CreateUserAndLoginAsync();
        var date = new DateOnly(2026, 11, 12);
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(2);
        var created = await CreateAttackAsync(token,
            CreateAttackRequest(start, end, 5, PainLocation.LeftSide, "Update"));

        using var clearedResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/migraine-attacks/{created.Id}", token,
            new UpdateMigraineAttackRequest
            {
                StartDate = date,
                EndDate = date.AddDays(1),
                Intensity = 7,
                PainLocation = PainLocation.LeftSide
            });
        Assert.Equal(HttpStatusCode.OK, clearedResponse.StatusCode);
        var cleared = await ReadJsonAsync<MigraineAttackResponse>(clearedResponse);
        Assert.NotNull(cleared);
        Assert.Equal(created.Id, cleared.Id);
        Assert.Equal(2, cleared.DurationDays);
        Assert.Null(cleared.StartAt);
        Assert.Null(cleared.EndAt);
        Assert.Null(cleared.DurationMinutes);

        using var readResponse = await SendAuthorizedAsync(
            HttpMethod.Get, $"/api/migraine-attacks/{created.Id}", token);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var stored = await ReadJsonAsync<MigraineAttackResponse>(readResponse);
        Assert.NotNull(stored);
        Assert.Null(stored.StartAt);
        Assert.Null(stored.EndAt);
        Assert.Equal(date.AddDays(1), stored.EndDate);

        using var restoredResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/migraine-attacks/{created.Id}", token,
            new UpdateMigraineAttackRequest
            {
                StartDate = date,
                EndDate = date,
                StartAt = start,
                EndAt = end,
                Intensity = 7,
                PainLocation = PainLocation.LeftSide
            });
        Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);
        var restored = await ReadJsonAsync<MigraineAttackResponse>(restoredResponse);
        Assert.NotNull(restored);
        Assert.Equal(created.Id, restored.Id);
        Assert.Equal(120d, restored.DurationMinutes);
        Assert.Equal(1, restored.DurationDays);
    }

    [Fact]
    public async Task CalendarFilter_ShouldIncludeOverlapsAndSeparateSameDayAttacks()
    {
        var token = await CreateUserAndLoginAsync();
        var otherToken = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 15);

        var crossingStart = await CreateAttackAsync(token,
            CalendarRequest(day.AddDays(-2), day));
        var crossingEnd = await CreateAttackAsync(token,
            CalendarRequest(day, day.AddDays(2)));
        var sameDay1 = await CreateAttackAsync(token, CalendarRequest(day, day));
        var sameDay2 = await CreateAttackAsync(token, CalendarRequest(day, day));
        await CreateAttackAsync(token,
            CalendarRequest(day.AddDays(-2), day.AddDays(-1)));
        await CreateAttackAsync(token,
            CalendarRequest(day.AddDays(1), day.AddDays(2)));
        await CreateAttackAsync(otherToken, CalendarRequest(day, day));

        Assert.NotEqual(sameDay1.Id, sameDay2.Id);

        const string period = "?fromDate=2026-11-15&toDate=2026-11-15";
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks" + period, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadJsonAsync<PagedResponse<MigraineAttackResponse>>(response);
        Assert.NotNull(page);
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(4, page.Items.Count);
        Assert.Contains(page.Items, x => x.Id == crossingStart.Id);
        Assert.Contains(page.Items, x => x.Id == crossingEnd.Id);
        Assert.Contains(page.Items, x => x.Id == sameDay1.Id);
        Assert.Contains(page.Items, x => x.Id == sameDay2.Id);

        using var statsResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics" + period, token);
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(statsResponse);
        Assert.NotNull(statistics);
        Assert.Equal(4, statistics.AttackCount);
        Assert.Null(statistics.AverageDurationMinutes);
    }

    [Fact]
    public async Task LegacyTimestampFilter_ShouldKeepStartTimeSemantics()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 16);
        var early = new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);
        var late = early.AddHours(4);

        await CreateAttackAsync(token, CreateAttackRequest(
            early, early.AddHours(5), 4, PainLocation.LeftSide, "Earlier start"));
        var matching = await CreateAttackAsync(token, CreateAttackRequest(
            late, late.AddHours(1), 8, PainLocation.LeftSide, "Matching"));
        var unknown = await CreateAttackAsync(token, CalendarRequest(day, day));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(-1), day));

        const string period = "?from=2026-11-16T12:00:00Z&to=2026-11-16T16:00:00Z";
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks" + period, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadJsonAsync<PagedResponse<MigraineAttackResponse>>(response);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, x => x.Id == matching.Id);
        Assert.Contains(page.Items, x => x.Id == unknown.Id);

        using var statsResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics" + period, token);
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(statsResponse);
        Assert.NotNull(statistics);
        Assert.Equal(2, statistics.AttackCount);
        Assert.Equal(60d, statistics.AverageDurationMinutes);
    }

    [Fact]
    public async Task Statistics_ShouldExcludeUnknownDurationsButCountAllAttacks()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 17);
        var start = new DateTimeOffset(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);

        await CreateAttackAsync(token, CreateAttackRequest(
            start, start.AddHours(2), 4, PainLocation.LeftSide, "120 min"));
        await CreateAttackAsync(token, CreateAttackRequest(
            start.AddHours(3), start.AddHours(7), 8, PainLocation.LeftSide, "240 min"));
        await CreateAttackAsync(token, CalendarRequest(day, day, intensity: 6));
        await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = day,
            EndDate = day,
            StartAt = start,
            Intensity = 2,
            PainLocation = PainLocation.LeftSide
        });

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(statistics);
        Assert.Equal(4, statistics.AttackCount);
        Assert.Equal(5d, statistics.AverageIntensity);
        Assert.Equal(180d, statistics.AverageDurationMinutes);
    }

    [Theory]
    [InlineData("/api/migraine-attacks")]
    [InlineData("/api/migraine-attacks/statistics")]
    public async Task MixedFilterModes_ShouldReturnBadRequest(string endpoint)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            endpoint + "?from=2026-11-01T00:00:00Z&toDate=2026-11-30",
            token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/migraine-attacks")]
    [InlineData("/api/migraine-attacks/statistics")]
    public async Task ReversedCalendarFilter_ShouldReturnBadRequest(string endpoint)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            endpoint + "?fromDate=2026-11-30&toDate=2026-11-01",
            token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CalendarCreate_WithMissingOrInvalidPeriod_ShouldReturnBadRequest()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 18);
        var time = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
        var requests = new[]
        {
            new CreateMigraineAttackRequest
            {
                Intensity = 5, PainLocation = PainLocation.LeftSide
            },
            new CreateMigraineAttackRequest
            {
                StartDate = day, StartAt = time, EndAt = time.AddHours(1),
                Intensity = 5, PainLocation = PainLocation.LeftSide
            },
            CalendarRequest(day, day.AddDays(-1)),
            new CreateMigraineAttackRequest
            {
                StartDate = day, EndDate = day, StartAt = time, EndAt = time.AddHours(-1),
                Intensity = 5, PainLocation = PainLocation.LeftSide
            }
        };

        foreach (var request in requests)
        {
            using var response = await SendAuthorizedAsync(
                HttpMethod.Post, "/api/migraine-attacks", token, request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var listResponse = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks", token);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await ReadJsonAsync<PagedResponse<MigraineAttackResponse>>(listResponse);
        Assert.NotNull(page);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task CalendarList_ShouldSortKnownStartTimesBeforeUnknownTimes()
    {
        var token = await CreateUserAndLoginAsync();
        var date = new DateOnly(2026, 11, 19);
        var early = new DateTimeOffset(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);
        var late = early.AddHours(4);

        var newest = await CreateAttackAsync(token, CreateAttackRequest(
            late, late.AddHours(1), 6, PainLocation.LeftSide, "Later start"));
        var oldest = await CreateAttackAsync(token, CreateAttackRequest(
            early, early.AddHours(1), 5, PainLocation.LeftSide, "Earlier start"));
        var unknown = await CreateAttackAsync(token, CalendarRequest(date, date));

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks?fromDate=2026-11-19&toDate=2026-11-19", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadJsonAsync<PagedResponse<MigraineAttackResponse>>(response);
        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(newest.Id, page.Items[0].Id);
        Assert.Equal(oldest.Id, page.Items[1].Id);
        Assert.Equal(unknown.Id, page.Items[2].Id);
    }

    [Fact]
    public async Task Calendar_ShouldClipOverlapsAggregateAndIsolateUsers()
    {
        var token = await CreateUserAndLoginAsync();
        var otherToken = await CreateUserAndLoginAsync();
        var from = new DateOnly(2026, 11, 10);
        var to = from.AddDays(4);

        var first = await CreateAttackAsync(token,
            CalendarRequest(from.AddDays(-2), from, 3));
        var second = await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = from,
            EndDate = from.AddDays(1),
            StartAt = new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero),
            EndAt = null,
            Intensity = 8,
            PainLocation = PainLocation.LeftSide
        });
        var third = await CreateAttackAsync(token,
            CalendarRequest(from.AddDays(1), from.AddDays(1), 6));
        var last = await CreateAttackAsync(token,
            CalendarRequest(to, to.AddDays(2), 4));

        await CreateAttackAsync(token,
            CalendarRequest(from.AddDays(-3), from.AddDays(-1), 10));
        await CreateAttackAsync(token,
            CalendarRequest(to.AddDays(1), to.AddDays(2), 10));
        await CreateAttackAsync(otherToken,
            CalendarRequest(from, to, 10));

        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            "/api/migraine-attacks/calendar?from=2026-11-10&to=2026-11-14", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var calendar = await ReadJsonAsync<MigraineAttackCalendarResponse>(response);
        Assert.NotNull(calendar);
        Assert.Equal(from, calendar.From);
        Assert.Equal(to, calendar.To);

        Assert.Collection(calendar.Days,
            day =>
            {
                Assert.Equal(from, day.Date);
                Assert.Equal(2, day.AttackCount);
                Assert.Equal(8, day.MaxIntensity);
                Assert.Equal(new[] { first.Id, second.Id }.OrderBy(x => x).ToArray(),
                    day.AttackIds.ToArray());
            },
            day =>
            {
                Assert.Equal(from.AddDays(1), day.Date);
                Assert.Equal(2, day.AttackCount);
                Assert.Equal(8, day.MaxIntensity);
                Assert.Equal(new[] { second.Id, third.Id }.OrderBy(x => x).ToArray(),
                    day.AttackIds.ToArray());
            },
            day =>
            {
                Assert.Equal(to, day.Date);
                Assert.Equal(1, day.AttackCount);
                Assert.Equal(4, day.MaxIntensity);
                Assert.Equal(last.Id, Assert.Single(day.AttackIds));
            });
    }

    [Theory]
    [InlineData("2026-11-10", "2026-11-10")]
    [InlineData("2026-01-01", "2027-01-01")]
    public async Task Calendar_ShouldReturnEmptyDaysForValidEmptyPeriod(string from, string to)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            $"/api/migraine-attacks/calendar?from={from}&to={to}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var calendar = await ReadJsonAsync<MigraineAttackCalendarResponse>(response);
        Assert.NotNull(calendar);
        Assert.Empty(calendar.Days);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?from=2026-11-10")]
    [InlineData("?to=2026-11-10")]
    [InlineData("?from=2026-11-11&to=2026-11-10")]
    [InlineData("?from=invalid&to=2026-11-10")]
    [InlineData("?from=2026-01-01&to=2027-01-02")]
    public async Task Calendar_ShouldRejectInvalidPeriod(string query)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            "/api/migraine-attacks/calendar" + query, token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_ShouldRequireAuthorization()
    {
        using var response = await _client.GetAsync(
            "/api/migraine-attacks/calendar?from=2026-11-10&to=2026-11-14");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Statistics_ShouldCountUniqueMigraineDays()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 10);

        await CreateAttackAsync(token, CalendarRequest(day, day.AddDays(2), 4));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(1), day.AddDays(3), 8));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(3), day.AddDays(4), 6));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(1), day.AddDays(1), 2));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(6), day.AddDays(6), 10));

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(statistics);
        Assert.Equal(5, statistics.AttackCount);
        Assert.Equal(6, statistics.MigraineDaysCount);
        Assert.Equal(2, statistics.MinIntensity);
        Assert.Equal(10, statistics.MaxIntensity);
        Assert.Equal(6d, statistics.AverageIntensity);
        Assert.Equal(0, statistics.KnownDurationAttackCount);
        Assert.Equal(5, statistics.UnknownDurationAttackCount);
        Assert.Null(statistics.AverageDurationMinutes);
    }

    [Fact]
    public async Task Statistics_ShouldClipMigraineDaysToPeriod()
    {
        var token = await CreateUserAndLoginAsync();
        await CreateAttackAsync(token, CalendarRequest(
            new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 17), 4));
        await CreateAttackAsync(token, CalendarRequest(
            new DateOnly(2026, 11, 18), new DateOnly(2026, 11, 24), 8));

        var cases = new[]
        {
            (Query: "?fromDate=2026-11-15&toDate=2026-11-20", Attacks: 2, Days: 6),
            (Query: "?fromDate=2026-11-15", Attacks: 2, Days: 10),
            (Query: "?toDate=2026-11-20", Attacks: 2, Days: 11),
            (Query: "?fromDate=2026-11-17&toDate=2026-11-17", Attacks: 1, Days: 1),
            (Query: "?fromDate=2026-11-25", Attacks: 0, Days: 0),
            (Query: "?from=2026-11-10T00:00:00Z&to=2026-11-10T23:59:59Z", Attacks: 1, Days: 8)
        };

        foreach (var item in cases)
        {
            using var response = await SendAuthorizedAsync(
                HttpMethod.Get, "/api/migraine-attacks/statistics" + item.Query, token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
            Assert.NotNull(statistics);
            Assert.Equal(item.Attacks, statistics.AttackCount);
            Assert.Equal(item.Days, statistics.MigraineDaysCount);
        }
    }

    [Fact]
    public async Task Statistics_ShouldCountKnownAndUnknownDurations()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 17);
        var start = new DateTimeOffset(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);

        await CreateAttackAsync(token, CreateAttackRequest(
            start, start.AddHours(2), 4, PainLocation.LeftSide, "120 minutes"));
        await CreateAttackAsync(token, CreateAttackRequest(
            start.AddHours(3), start.AddHours(7), 8, PainLocation.LeftSide, "240 minutes"));
        await CreateAttackAsync(token, CalendarRequest(day, day, 6));
        await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = day,
            EndDate = day,
            StartAt = start,
            Intensity = 2,
            PainLocation = PainLocation.LeftSide
        });
        await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = day,
            EndDate = day,
            EndAt = start.AddHours(2),
            Intensity = 7,
            PainLocation = PainLocation.LeftSide
        });

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(statistics);
        Assert.Equal(5, statistics.AttackCount);
        Assert.Equal(1, statistics.MigraineDaysCount);
        Assert.Equal(2, statistics.KnownDurationAttackCount);
        Assert.Equal(3, statistics.UnknownDurationAttackCount);
        Assert.Equal(180d, statistics.AverageDurationMinutes);
        Assert.Equal(2, statistics.MinIntensity);
        Assert.Equal(8, statistics.MaxIntensity);
    }

    [Fact]
    public async Task Statistics_ShouldReturnEmptyMetricsWithoutAttacks()
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(statistics);
        Assert.Equal(0, statistics.AttackCount);
        Assert.Equal(0, statistics.MigraineDaysCount);
        Assert.Equal(0d, statistics.AverageIntensity);
        Assert.Equal(0, statistics.KnownDurationAttackCount);
        Assert.Equal(0, statistics.UnknownDurationAttackCount);
        Assert.Null(statistics.MinIntensity);
        Assert.Null(statistics.MaxIntensity);
        Assert.Null(statistics.AverageDurationMinutes);
        Assert.Empty(statistics.Symptoms);
        Assert.Empty(statistics.Triggers);
        Assert.Empty(statistics.Medications);
    }

    [Fact]
    public async Task Statistics_ShouldExcludeOtherUsersMetrics()
    {
        var token = await CreateUserAndLoginAsync();
        var otherToken = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 15);
        await CreateAttackAsync(token, CalendarRequest(day, day.AddDays(1), 4));
        await CreateAttackAsync(otherToken, CalendarRequest(day, day.AddDays(9), 10));

        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/migraine-attacks/statistics", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(statistics);
        Assert.Equal(1, statistics.AttackCount);
        Assert.Equal(2, statistics.MigraineDaysCount);
        Assert.Equal(4, statistics.MinIntensity);
        Assert.Equal(4, statistics.MaxIntensity);
        Assert.Equal(4d, statistics.AverageIntensity);
        Assert.Equal(0, statistics.KnownDurationAttackCount);
        Assert.Equal(1, statistics.UnknownDurationAttackCount);
    }

    [Fact]
    public async Task ListDurationFilter_ShouldIncludeBoundariesAndExcludeUnknown()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 22, 0, 0, TimeSpan.Zero);
        var low = await CreateSortingAttackAsync(token, start, 60, 4);
        var high = await CreateSortingAttackAsync(token, start, 240, 6);
        await CreateSortingAttackAsync(token, start, 30, 7);
        await CreateSortingAttackAsync(token, start, 241, 8);
        await CreateAttackAsync(token, CalendarRequest(new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13)));
        await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = new DateOnly(2026, 11, 12), EndDate = new DateOnly(2026, 11, 13),
            StartAt = start, Intensity = 5, PainLocation = PainLocation.LeftSide
        });
        var result = await GetSortedAttacksAsync(token, "minDurationMinutes=60&maxDurationMinutes=240&sortBy=duration&sortDirection=asc");
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { low.Id, high.Id }, result.Items.Select(x => x.Id).ToArray());
    }

    [Theory]
    [InlineData("minDurationMinutes=60", 120)]
    [InlineData("maxDurationMinutes=60", 30)]
    public async Task ListDurationFilter_ShouldSupportSingleBound(string query, int expectedMinutes)
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        var shortAttack = await CreateSortingAttackAsync(token, start, 30, 4);
        var longAttack = await CreateSortingAttackAsync(token, start, 120, 4);
        var result = await GetSortedAttacksAsync(token, query);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(expectedMinutes == 30 ? shortAttack.Id : longAttack.Id, Assert.Single(result.Items).Id);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task ListSorting_ShouldSortIntensity(string direction)
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        var high = await CreateAttackAsync(token, CalendarRequest(day, day, 9));
        var low = await CreateAttackAsync(token, CalendarRequest(day.AddDays(1), day.AddDays(1), 2));
        var medium = await CreateAttackAsync(token, CalendarRequest(day.AddDays(2), day.AddDays(2), 5));
        var result = await GetSortedAttacksAsync(token, $"sortBy=intensity&sortDirection={direction}");
        var expected = direction == "asc" ? new[] { low.Id, medium.Id, high.Id } : new[] { high.Id, medium.Id, low.Id };
        Assert.Equal(expected, result.Items.Select(x => x.Id).ToArray());
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task ListSorting_ShouldSortDurationWithUnknownLast(string direction)
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 22, 0, 0, TimeSpan.Zero);
        var unknown = await CreateAttackAsync(token, CalendarRequest(new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 15)));
        var longAttack = await CreateSortingAttackAsync(token, start, 240, 4);
        var zero = await CreateSortingAttackAsync(token, start, 0, 4);
        var shortAttack = await CreateSortingAttackAsync(token, start, 60, 4);
        var result = await GetSortedAttacksAsync(token, $"sortBy=duration&sortDirection={direction}");
        var expected = direction == "asc"
            ? new[] { zero.Id, shortAttack.Id, longAttack.Id, unknown.Id }
            : new[] { longAttack.Id, shortAttack.Id, zero.Id, unknown.Id };
        Assert.Equal(expected, result.Items.Select(x => x.Id).ToArray());
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task ListSorting_ShouldSortCalendarDates(string direction)
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        var older = await CreateAttackAsync(token, CalendarRequest(day, day));
        var unknown = await CreateAttackAsync(token, CalendarRequest(day.AddDays(1), day.AddDays(1)));
        var early = await CreateSortingAttackAsync(token, new DateTimeOffset(2026, 11, 13, 8, 0, 0, TimeSpan.Zero), 30, 5);
        var late = await CreateSortingAttackAsync(token, new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero), 30, 5);
        var result = await GetSortedAttacksAsync(token, $"sortBy=date&sortDirection={direction}");
        var expected = direction == "asc"
            ? new[] { older.Id, early.Id, late.Id, unknown.Id }
            : new[] { late.Id, early.Id, unknown.Id, older.Id };
        Assert.Equal(expected, result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task ListSorting_ShouldFilterBeforePaginationAndIsolateUsers()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        await CreateSortingAttackAsync(token, start, 120, 8);
        var medium = await CreateSortingAttackAsync(token, start, 90, 6);
        var low = await CreateSortingAttackAsync(token, start, 60, 4);
        await CreateSortingAttackAsync(token, start, 30, 10);
        await CreateSortingAttackAsync(other, start, 90, 7);
        await CreateSortingAttackAsync(token, start.AddDays(5), 90, 9);
        await CreateSortingAttackAsync(token, start, 90, 2);
        var query = "fromDate=2026-11-12&toDate=2026-11-12&minIntensity=4&minDurationMinutes=60&sortBy=intensity&sortDirection=desc&pageSize=1";
        var second = await GetSortedAttacksAsync(token, query + "&page=2");
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
        Assert.Equal(medium.Id, Assert.Single(second.Items).Id);
        var third = await GetSortedAttacksAsync(token, query + "&page=3");
        Assert.Equal(low.Id, Assert.Single(third.Items).Id);
        var fourth = await GetSortedAttacksAsync(token, query + "&page=4");
        Assert.Equal(3, fourth.TotalCount);
        Assert.Empty(fourth.Items);
    }

    [Fact]
    public async Task ListDurationFilter_ShouldDistinguishZeroAndUnknown()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        var zero = await CreateSortingAttackAsync(token, start, 0, 5);
        await CreateSortingAttackAsync(token, start, 1, 5);
        await CreateAttackAsync(token, CalendarRequest(new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 12)));
        var result = await GetSortedAttacksAsync(token, "minDurationMinutes=0&maxDurationMinutes=0");
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(zero.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListDurationFilter_ShouldReturnEmptyWhenOnlyUnknown()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day, day));
        var result = await GetSortedAttacksAsync(token, "minDurationMinutes=0");
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData("minDurationMinutes=-1")]
    [InlineData("maxDurationMinutes=-1")]
    [InlineData("minDurationMinutes=120&maxDurationMinutes=60")]
    [InlineData("minDurationMinutes=NaN")]
    [InlineData("maxDurationMinutes=Infinity")]
    [InlineData("sortBy=invalid")]
    [InlineData("sortDirection=invalid")]
    public async Task ListSorting_ShouldRejectInvalidParameters(string query)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"/api/migraine-attacks?{query}", token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListSorting_ShouldKeepPaginationStableForEqualValues()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 3; i++)
            await CreateSortingAttackAsync(token, start, 60, 5);
        const string query = "sortBy=duration&sortDirection=asc";
        var all = await GetSortedAttacksAsync(token, query);
        var ids = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await GetSortedAttacksAsync(token, query + $"&pageSize=1&page={page}");
            Assert.Equal(3, result.TotalCount);
            ids.Add(Assert.Single(result.Items).Id);
        }
        Assert.Equal(all.Items.Select(x => x.Id).ToArray(), ids.ToArray());
        Assert.Equal(3, ids.Distinct().Count());
    }

    private Task<MigraineAttackResponse> CreateSortingAttackAsync(
        string token, DateTimeOffset start, double durationMinutes, int intensity)
    {
        return CreateAttackAsync(token, CreateAttackRequest(
            start, start.AddMinutes(durationMinutes), intensity, PainLocation.LeftSide, "Sorting test"));
    }

    private async Task<PagedResponse<MigraineAttackResponse>> GetSortedAttacksAsync(string token, string query)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"/api/migraine-attacks?{query}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadJsonAsync<PagedResponse<MigraineAttackResponse>>(response);
        Assert.NotNull(result);
        return result;
    }

    [Fact]
    public async Task ChartData_ShouldReturnAllIntensityLevelsInOrder()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day, day, 1));
        await CreateAttackAsync(token, CalendarRequest(day, day, 5));
        await CreateAttackAsync(token, CalendarRequest(day, day, 5));
        await CreateAttackAsync(token, CalendarRequest(day, day, 10));

        var stats = await GetChartStatisticsAsync(token);
        Assert.Equal(Enumerable.Range(1, 10), stats.IntensityDistribution.Select(x => x.Intensity));
        Assert.Equal(new[] { 1, 0, 0, 0, 2, 0, 0, 0, 0, 1 }, stats.IntensityDistribution.Select(x => x.Count));
        Assert.Equal(stats.AttackCount, stats.IntensityDistribution.Sum(x => x.Count));
    }

    [Fact]
    public async Task ChartData_WithoutAttacks_ShouldReturnZeroBinsAndEmptyTrend()
    {
        var token = await CreateUserAndLoginAsync();
        var stats = await GetChartStatisticsAsync(token);
        Assert.Equal(0, stats.AttackCount);
        Assert.Equal(Enumerable.Range(1, 10), stats.IntensityDistribution.Select(x => x.Intensity));
        Assert.All(stats.IntensityDistribution, x => Assert.Equal(0, x.Count));
        Assert.Empty(stats.DailyTrend);
    }

    [Fact]
    public async Task ChartData_ShouldAggregateByStartDateAndIgnoreUnknownDurations()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(2), day.AddDays(2), 4));
        await CreateAttackAsync(token, CreateAttackRequest(start, start.AddHours(1), 3, PainLocation.LeftSide, "first"));
        await CreateAttackAsync(token, CreateAttackRequest(start.AddHours(12), start.AddHours(15), 6, PainLocation.LeftSide, "overnight"));
        await CreateAttackAsync(token, CalendarRequest(day, day, 9));

        var stats = await GetChartStatisticsAsync(token);
        Assert.Equal(new[] { day, day.AddDays(2) }, stats.DailyTrend.Select(x => x.Date));
        var first = stats.DailyTrend[0];
        Assert.Equal(3, first.AttackCount);
        Assert.Equal(6d, first.AverageIntensity);
        Assert.Equal(9, first.MaxIntensity);
        Assert.Equal(2, first.KnownDurationAttackCount);
        Assert.Equal(120d, first.AverageDurationMinutes);
        Assert.Equal(180d, first.MaxDurationMinutes);
        var second = stats.DailyTrend[1];
        Assert.Equal(1, second.AttackCount);
        Assert.Equal(0, second.KnownDurationAttackCount);
        Assert.Null(second.AverageDurationMinutes);
        Assert.Null(second.MaxDurationMinutes);
    }

    [Fact]
    public async Task ChartData_ShouldKeepCalendarOverlapMetricsButExcludeEarlierStartsFromTrend()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(-1), day, 8));
        await CreateAttackAsync(token, CalendarRequest(day, day.AddDays(1), 6));
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(1), day.AddDays(1), 10));

        var stats = await GetChartStatisticsAsync(token, "?fromDate=2026-11-12&toDate=2026-11-12");
        Assert.Equal(2, stats.AttackCount);
        Assert.Equal(1, stats.MigraineDaysCount);
        Assert.Equal(1, stats.IntensityDistribution.Single(x => x.Intensity == 8).Count);
        Assert.Equal(1, stats.IntensityDistribution.Single(x => x.Intensity == 6).Count);
        Assert.Equal(0, stats.IntensityDistribution.Single(x => x.Intensity == 10).Count);
        var point = Assert.Single(stats.DailyTrend);
        Assert.Equal(day, point.Date);
        Assert.Equal(1, point.AttackCount);
        Assert.Equal(6d, point.AverageIntensity);
    }

    [Fact]
    public async Task ChartData_ShouldRespectLegacyTimestampFilters()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        await CreateAttackAsync(token, CreateAttackRequest(start, start.AddHours(1), 4, PainLocation.LeftSide, "included"));
        await CreateAttackAsync(token, CreateAttackRequest(start.AddHours(-2), start, 9, PainLocation.LeftSide, "excluded"));

        var stats = await GetChartStatisticsAsync(token, "?from=2026-11-12T10:00:00Z&to=2026-11-12T10:00:00Z");
        Assert.Equal(1, stats.AttackCount);
        Assert.Equal(1, stats.IntensityDistribution.Single(x => x.Intensity == 4).Count);
        Assert.Equal(0, stats.IntensityDistribution.Single(x => x.Intensity == 9).Count);
        Assert.Equal(60d, Assert.Single(stats.DailyTrend).AverageDurationMinutes);
    }

    [Fact]
    public async Task ChartData_ShouldExcludeOtherUsers()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day, day, 4));
        await CreateAttackAsync(other, CalendarRequest(day, day, 10));
        await CreateAttackAsync(other, CalendarRequest(day.AddDays(1), day.AddDays(1), 10));

        var stats = await GetChartStatisticsAsync(token);
        Assert.Equal(1, stats.AttackCount);
        Assert.Equal(0, stats.IntensityDistribution.Single(x => x.Intensity == 10).Count);
        var point = Assert.Single(stats.DailyTrend);
        Assert.Equal(day, point.Date);
        Assert.Equal(1, point.AttackCount);
    }

    [Fact]
    public async Task ChartData_ShouldDistinguishZeroDurationFromUnknownDuration()
    {
        var token = await CreateUserAndLoginAsync();
        var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CreateAttackRequest(start, start, 5, PainLocation.LeftSide, "zero"));
        await CreateAttackAsync(token, new CreateMigraineAttackRequest
        {
            StartDate = day,
            EndDate = day,
            StartAt = start,
            Intensity = 5,
            PainLocation = PainLocation.LeftSide
        });

        var stats = await GetChartStatisticsAsync(token);
        var point = Assert.Single(stats.DailyTrend);
        Assert.Equal(2, point.AttackCount);
        Assert.Equal(1, point.KnownDurationAttackCount);
        Assert.Equal(0d, point.AverageDurationMinutes);
        Assert.Equal(0d, point.MaxDurationMinutes);
    }

    [Fact]
    public async Task ChartData_WithoutStartsInRange_ShouldNotInventStartDates()
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateOnly(2026, 11, 12);
        await CreateAttackAsync(token, CalendarRequest(day.AddDays(-2), day.AddDays(1), 7));

        var stats = await GetChartStatisticsAsync(token, "?fromDate=2026-11-12&toDate=2026-11-12");
        Assert.Equal(1, stats.AttackCount);
        Assert.Equal(1, stats.MigraineDaysCount);
        Assert.Equal(1, stats.IntensityDistribution.Single(x => x.Intensity == 7).Count);
        Assert.Empty(stats.DailyTrend);
    }

    private async Task<MigraineAttackStatisticsResponse> GetChartStatisticsAsync(string token, string query = "")
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/migraine-attacks/statistics" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadJsonAsync<MigraineAttackStatisticsResponse>(response);
        Assert.NotNull(result);
        return result;
    }

    private static CreateMigraineAttackRequest CalendarRequest(
        DateOnly start,
        DateOnly end,
        int intensity = 5)
    {
        return new CreateMigraineAttackRequest
        {
            StartDate = start,
            EndDate = end,
            Intensity = intensity,
            PainLocation = PainLocation.LeftSide
        };
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
