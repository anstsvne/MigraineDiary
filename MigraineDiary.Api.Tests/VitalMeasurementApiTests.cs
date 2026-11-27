using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Contracts.Common;
using MigraineDiary.Api.Contracts.VitalMeasurements;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class VitalMeasurementApiTests
{
    private readonly HttpClient _client;

    public VitalMeasurementApiTests(
        ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Create_ShouldCreateMeasurement()
    {
        var token =
            await CreateUserAndLoginAsync();

        var request =
            new CreateVitalMeasurementRequest
            {
                SystolicPressure = 120,
                DiastolicPressure = 80,
                Pulse = 70,

                MeasuredAt =
                    new DateTimeOffset(
                        2026, 11, 10,
                        10, 0, 0,
                        TimeSpan.Zero),

                Notes =
                    "Test measurement"
            };

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/vital-measurements",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var measurement =
            await response.Content
                .ReadFromJsonAsync<
                    VitalMeasurementResponse>();

        Assert.NotNull(measurement);

        Assert.NotEqual(
            Guid.Empty,
            measurement.Id);

        Assert.Equal(
            120,
            measurement.SystolicPressure);

        Assert.Equal(
            80,
            measurement.DiastolicPressure);

        Assert.Equal(
            70,
            measurement.Pulse);

        Assert.Equal(
            "Test measurement",
            measurement.Notes);
    }

    [Fact]
    public async Task GetById_ShouldReturnCreatedMeasurement()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                token,
                125,
                82,
                74,
                "Get test");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/vital-measurements/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var measurement =
            await response.Content
                .ReadFromJsonAsync<
                    VitalMeasurementResponse>();

        Assert.NotNull(measurement);

        Assert.Equal(
            created.Id,
            measurement.Id);

        Assert.Equal(
            125,
            measurement.SystolicPressure);
    }

    [Fact]
    public async Task Update_ShouldChangeMeasurement()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                token,
                120,
                80,
                70,
                "Before update");

        var request =
            new UpdateVitalMeasurementRequest
            {
                SystolicPressure = 130,
                DiastolicPressure = 85,
                Pulse = 76,

                MeasuredAt =
                    new DateTimeOffset(
                        2026, 11, 11,
                        12, 0, 0,
                        TimeSpan.Zero),

                Notes =
                    "After update"
            };

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/vital-measurements/{created.Id}",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var measurement =
            await response.Content
                .ReadFromJsonAsync<
                    VitalMeasurementResponse>();

        Assert.NotNull(measurement);

        Assert.Equal(
            130,
            measurement.SystolicPressure);

        Assert.Equal(
            85,
            measurement.DiastolicPressure);

        Assert.Equal(
            76,
            measurement.Pulse);

        Assert.Equal(
            "After update",
            measurement.Notes);
    }

    [Fact]
    public async Task Delete_ShouldRemoveMeasurement()
    {
        var token =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                token,
                118,
                78,
                68,
                "Delete test");

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/vital-measurements/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/vital-measurements/{created.Id}",
                token);

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherUser_ShouldReturnNotFound()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                firstToken,
                122,
                81,
                71,
                "Private measurement");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/vital-measurements/{created.Id}",
                secondToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherUser_ShouldReturnNotFound()
    {
        var ownerToken =
            await CreateUserAndLoginAsync();

        var anotherUserToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                ownerToken,
                120,
                80,
                70,
                "Owner measurement");

        var updateRequest =
            new UpdateVitalMeasurementRequest
            {
                SystolicPressure = 150,
                DiastolicPressure = 95,
                Pulse = 90,

                MeasuredAt =
                    new DateTimeOffset(
                        2026, 11, 22,
                        12, 0, 0,
                        TimeSpan.Zero),

                Notes =
                    "Unauthorized update"
            };

        var updateResponse =
            await SendAuthorizedAsync(
                HttpMethod.Put,
                $"/api/vital-measurements/{created.Id}",
                anotherUserToken,
                updateRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            updateResponse.StatusCode);

        var ownerResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/vital-measurements/{created.Id}",
                ownerToken);

        Assert.Equal(
            HttpStatusCode.OK,
            ownerResponse.StatusCode);

        var unchanged =
            await ownerResponse.Content
                .ReadFromJsonAsync<
                    VitalMeasurementResponse>();

        Assert.NotNull(unchanged);

        Assert.Equal(
            120,
            unchanged.SystolicPressure);

        Assert.Equal(
            80,
            unchanged.DiastolicPressure);

        Assert.Equal(
            70,
            unchanged.Pulse);

        Assert.Equal(
            "Owner measurement",
            unchanged.Notes);
    }

    [Fact]
    public async Task Delete_ForAnotherUser_ShouldReturnNotFound()
    {
        var ownerToken =
            await CreateUserAndLoginAsync();

        var anotherUserToken =
            await CreateUserAndLoginAsync();

        var created =
            await CreateMeasurementAsync(
                ownerToken,
                125,
                82,
                72,
                "Protected measurement");

        var deleteResponse =
            await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/vital-measurements/{created.Id}",
                anotherUserToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            deleteResponse.StatusCode);

        var ownerResponse =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/vital-measurements/{created.Id}",
                ownerToken);

        Assert.Equal(
            HttpStatusCode.OK,
            ownerResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyCurrentUsersMeasurements()
    {
        var firstToken =
            await CreateUserAndLoginAsync();

        var secondToken =
            await CreateUserAndLoginAsync();

        var first =
            await CreateMeasurementAsync(
                firstToken,
                121,
                79,
                69,
                "First user");

        await CreateMeasurementAsync(
            secondToken,
            140,
            90,
            85,
            "Second user");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/vital-measurements",
                firstToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<VitalMeasurementResponse>>();

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
            first.Id,
            page.Items[0].Id);
    }

    [Fact]
    public async Task Pagination_ShouldReturnRequestedPage()
    {
        var token =
            await CreateUserAndLoginAsync();

        var oldest =
            await CreateMeasurementAsync(
                token,
                120,
                80,
                70,
                "Oldest",
                new DateTimeOffset(
                    2026, 11, 10,
                    8, 0, 0,
                    TimeSpan.Zero));

        await CreateMeasurementAsync(
            token,
            121,
            81,
            71,
            "Middle",
            new DateTimeOffset(
                2026, 11, 11,
                8, 0, 0,
                TimeSpan.Zero));

        await CreateMeasurementAsync(
            token,
            122,
            82,
            72,
            "Newest",
            new DateTimeOffset(
                2026, 11, 12,
                8, 0, 0,
                TimeSpan.Zero));

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/vital-measurements?page=2&pageSize=2",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<VitalMeasurementResponse>>();

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
    public async Task Pagination_WithInvalidPageSize_ShouldReturnBadRequest()
    {
        var token =
            await CreateUserAndLoginAsync();

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/vital-measurements?page=1&pageSize=101",
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

        await CreateMeasurementAsync(
            token,
            120,
            80,
            70,
            "Only measurement");

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/vital-measurements?page=10&pageSize=5",
                token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<VitalMeasurementResponse>>();

        Assert.NotNull(page);

        Assert.Equal(
            10,
            page.Page);

        Assert.Equal(
            5,
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
    public async Task Create_WithInvalidPressure_ShouldReturnBadRequest()
    {
        var token =
            await CreateUserAndLoginAsync();

        var request =
            new CreateVitalMeasurementRequest
            {
                SystolicPressure = 0,
                DiastolicPressure = 80,
                Pulse = 70,

                MeasuredAt =
                    DateTimeOffset.UtcNow
            };

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/vital-measurements",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Statistics_ShouldAggregateAllMeasurements()
    {
        var token = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 110, 70, 65, "first");
        await CreateMeasurementAsync(token, 126, 81, 76, "second");
        await CreateMeasurementAsync(token, 140, 90, 85, "third");

        var stats = await GetStatisticsAsync(token);

        Assert.Equal(3, stats.MeasurementCount);
        Assert.Equal(125.33d, stats.AverageSystolicPressure);
        Assert.Equal(110, stats.MinSystolicPressure);
        Assert.Equal(140, stats.MaxSystolicPressure);
        Assert.Equal(80.33d, stats.AverageDiastolicPressure);
        Assert.Equal(70, stats.MinDiastolicPressure);
        Assert.Equal(90, stats.MaxDiastolicPressure);
        Assert.Equal(75.33d, stats.AveragePulse);
        Assert.Equal(65, stats.MinPulse);
        Assert.Equal(85, stats.MaxPulse);
    }

    [Fact]
    public async Task Statistics_WithSingleMeasurement_ShouldReturnSameValues()
    {
        var token = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 123, 78, 69, "single");
        var stats = await GetStatisticsAsync(token);

        Assert.Equal(1, stats.MeasurementCount);
        Assert.Equal(123d, stats.AverageSystolicPressure);
        Assert.Equal(123, stats.MinSystolicPressure);
        Assert.Equal(123, stats.MaxSystolicPressure);
        Assert.Equal(78d, stats.AverageDiastolicPressure);
        Assert.Equal(78, stats.MinDiastolicPressure);
        Assert.Equal(78, stats.MaxDiastolicPressure);
        Assert.Equal(69d, stats.AveragePulse);
        Assert.Equal(69, stats.MinPulse);
        Assert.Equal(69, stats.MaxPulse);
    }

    [Fact]
    public async Task Statistics_WithoutMeasurements_ShouldReturnNullMetrics()
    {
        var token = await CreateUserAndLoginAsync();
        AssertEmptyStatistics(await GetStatisticsAsync(token));
    }

    [Fact]
    public async Task Statistics_ShouldExcludeOtherUsersMeasurements()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 120, 80, 70, "own");
        await CreateMeasurementAsync(other, 190, 110, 150, "foreign");

        var stats = await GetStatisticsAsync(token);
        Assert.Equal(1, stats.MeasurementCount);
        Assert.Equal(120d, stats.AverageSystolicPressure);
        Assert.Equal(80, stats.MaxDiastolicPressure);
        Assert.Equal(70, stats.MaxPulse);
    }

    [Theory]
    [InlineData("?fromDate=2026-11-12&toDate=2026-11-12", 2, 125)]
    [InlineData("?fromDate=2026-11-12", 3, 130)]
    [InlineData("?toDate=2026-11-12", 3, 120)]
    [InlineData("?from=2026-11-12T03:00:00%2B03:00&to=2026-11-12T03:00:00%2B03:00", 1, 120)]
    public async Task Statistics_ShouldRespectPeriodBoundaries(
        string query, int expectedCount, int expectedAverage)
    {
        var token = await CreateUserAndLoginAsync();
        var midnight = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        await CreateMeasurementAsync(token, 110, 70, 60, "before", midnight.AddSeconds(-1));
        await CreateMeasurementAsync(token, 120, 80, 70, "first", midnight);
        await CreateMeasurementAsync(token, 130, 85, 80, "last", midnight.AddDays(1).AddMilliseconds(-1));
        await CreateMeasurementAsync(token, 140, 90, 90, "after", midnight.AddDays(1));

        var stats = await GetStatisticsAsync(token, query);
        Assert.Equal(expectedCount, stats.MeasurementCount);
        Assert.Equal((double)expectedAverage, stats.AverageSystolicPressure);
    }

    [Fact]
    public async Task Statistics_WithNoMeasurementsInPeriod_ShouldReturnNullMetrics()
    {
        var token = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 120, 80, 70, "outside");
        AssertEmptyStatistics(await GetStatisticsAsync(token, "?fromDate=2026-11-20"));
    }

    [Theory]
    [InlineData("?fromDate=2026-11-13&toDate=2026-11-12")]
    [InlineData("?from=2026-11-13T00:00:00Z&to=2026-11-12T00:00:00Z")]
    [InlineData("?fromDate=2026-11-12&to=2026-11-13T00:00:00Z")]
    [InlineData("?fromDate=not-a-date")]
    public async Task Statistics_WithInvalidPeriod_ShouldReturnBadRequest(string query)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/vital-measurements/statistics" + query, token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Statistics_ShouldRequireAuthorization()
    {
        using var response = await _client.GetAsync("/api/vital-measurements/statistics");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("fromDate=2026-11-12&toDate=2026-11-12", "last,first")]
    [InlineData("fromDate=2026-11-12", "after,last,first")]
    [InlineData("toDate=2026-11-12", "last,first,before")]
    [InlineData("from=2026-11-12T03:00:00%2B03:00&to=2026-11-12T03:00:00%2B03:00", "first")]
    public async Task List_ShouldFilterPeriod(string query, string expected)
    {
        var token = await CreateUserAndLoginAsync();
        var midnight = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        await CreateMeasurementAsync(token, 110, 70, 60, "before", midnight.AddSeconds(-1));
        await CreateMeasurementAsync(token, 120, 80, 70, "first", midnight);
        await CreateMeasurementAsync(token, 130, 85, 80, "last", midnight.AddDays(1).AddMilliseconds(-1));
        await CreateMeasurementAsync(token, 140, 90, 90, "after", midnight.AddDays(1));
        var result = await GetFilteredMeasurementsAsync(token, query);
        Assert.Equal(expected.Split(','), result.Items.Select(x => x.Notes).ToArray());
        Assert.Equal(expected.Split(',').Length, result.TotalCount);
    }

    [Theory]
    [InlineData("minSystolicPressure=120", "C,B")]
    [InlineData("maxSystolicPressure=120", "B,A")]
    [InlineData("minDiastolicPressure=80", "C,B")]
    [InlineData("maxDiastolicPressure=80", "B,A")]
    [InlineData("minPulse=70", "C,B")]
    [InlineData("maxPulse=70", "B,A")]
    [InlineData("minSystolicPressure=120&maxSystolicPressure=120&minDiastolicPressure=80&maxDiastolicPressure=80&minPulse=70&maxPulse=70", "B")]
    public async Task List_ShouldFilterNumericRanges(string query, string expected)
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        await CreateMeasurementAsync(token, 110, 70, 60, "A", day);
        await CreateMeasurementAsync(token, 120, 80, 70, "B", day.AddHours(1));
        await CreateMeasurementAsync(token, 130, 90, 80, "C", day.AddHours(2));
        var result = await GetFilteredMeasurementsAsync(token, query);
        Assert.Equal(expected.Split(','), result.Items.Select(x => x.Notes).ToArray());
        Assert.Equal(expected.Split(',').Length, result.TotalCount);
    }

    [Theory]
    [InlineData("date", "asc", "A,B,C")]
    [InlineData("date", "desc", "C,B,A")]
    [InlineData("systolicPressure", "asc", "B,C,A")]
    [InlineData("systolicPressure", "desc", "A,C,B")]
    [InlineData("diastolicPressure", "asc", "C,A,B")]
    [InlineData("diastolicPressure", "desc", "B,A,C")]
    [InlineData("pulse", "asc", "B,A,C")]
    [InlineData("pulse", "desc", "C,A,B")]
    public async Task List_ShouldSortByRequestedField(string field, string direction, string expected)
    {
        var token = await CreateUserAndLoginAsync();
        var day = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        await CreateMeasurementAsync(token, 140, 80, 75, "A", day);
        await CreateMeasurementAsync(token, 110, 90, 60, "B", day.AddHours(1));
        await CreateMeasurementAsync(token, 125, 70, 90, "C", day.AddHours(2));
        var result = await GetFilteredMeasurementsAsync(token, $"sortBy={field}&sortDirection={direction}");
        Assert.Equal(expected.Split(','), result.Items.Select(x => x.Notes).ToArray());
    }

    [Fact]
    public async Task List_ShouldFilterAttackLinksAndBodyPosition()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        var firstAttack = await CreateFilterAttackAsync(token);
        var secondAttack = await CreateFilterAttackAsync(token);
        var foreignAttack = await CreateFilterAttackAsync(other);
        await CreateFilterMeasurementAsync(token, firstAttack, "Sitting", "linked sitting");
        await CreateFilterMeasurementAsync(token, secondAttack, "Standing", "linked standing");
        await CreateFilterMeasurementAsync(token, null, "Lying", "unlinked lying");
        await CreateMeasurementAsync(token, 120, 80, 70, "unknown position");
        await CreateFilterMeasurementAsync(other, foreignAttack, "Sitting", "foreign");

        Assert.Equal(2, (await GetFilteredMeasurementsAsync(token, "hasMigraineAttack=true")).TotalCount);
        Assert.Equal(2, (await GetFilteredMeasurementsAsync(token, "hasMigraineAttack=false")).TotalCount);
        Assert.Equal("linked sitting", Assert.Single((await GetFilteredMeasurementsAsync(token, $"migraineAttackId={firstAttack}&hasMigraineAttack=true&bodyPosition=Sitting")).Items).Notes);
        Assert.Equal("linked standing", Assert.Single((await GetFilteredMeasurementsAsync(token, "bodyPosition=Standing")).Items).Notes);
        Assert.Equal("unlinked lying", Assert.Single((await GetFilteredMeasurementsAsync(token, "bodyPosition=Lying&hasMigraineAttack=false")).Items).Notes);
        Assert.Empty((await GetFilteredMeasurementsAsync(token, $"migraineAttackId={foreignAttack}")).Items);
        Assert.Empty((await GetFilteredMeasurementsAsync(token, $"migraineAttackId={Guid.NewGuid()}")).Items);
    }

    [Fact]
    public async Task List_ShouldFilterAndSortBeforePaginationAndIsolateUsers()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 100, 70, 60, "excluded");
        await CreateMeasurementAsync(token, 130, 80, 90, "A");
        await CreateMeasurementAsync(token, 140, 85, 65, "B");
        await CreateMeasurementAsync(token, 150, 90, 75, "C");
        await CreateMeasurementAsync(other, 160, 95, 50, "foreign");
        var query = "minSystolicPressure=130&sortBy=pulse&sortDirection=asc&pageSize=2";
        var first = await GetFilteredMeasurementsAsync(token, query + "&page=1");
        var second = await GetFilteredMeasurementsAsync(token, query + "&page=2");
        var third = await GetFilteredMeasurementsAsync(token, query + "&page=3");
        Assert.Equal(new[] { "B", "C" }, first.Items.Select(x => x.Notes).ToArray());
        Assert.Equal("A", Assert.Single(second.Items).Notes);
        Assert.Empty(third.Items);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, third.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, third.TotalPages);
    }

    [Fact]
    public async Task List_ShouldKeepStableOrderingForTiedValues()
    {
        var token = await CreateUserAndLoginAsync();
        for (var i = 0; i < 3; i++)
            await CreateMeasurementAsync(token, 120, 80, 70, $"tie-{i}");
        const string query = "sortBy=pulse&sortDirection=asc";
        var all = await GetFilteredMeasurementsAsync(token, query);
        var pageIds = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await GetFilteredMeasurementsAsync(token, query + $"&pageSize=1&page={page}");
            pageIds.Add(Assert.Single(result.Items).Id);
        }
        Assert.Equal(all.Items.Select(x => x.Id).ToArray(), pageIds.ToArray());
        Assert.Equal(3, pageIds.Distinct().Count());
    }

    [Fact]
    public async Task List_WithNoMatches_ShouldReturnEmptyPage()
    {
        var token = await CreateUserAndLoginAsync();
        await CreateMeasurementAsync(token, 120, 80, 70, "outside");
        var result = await GetFilteredMeasurementsAsync(token, "minPulse=100");
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Theory]
    [InlineData("fromDate=2026-11-13&toDate=2026-11-12")]
    [InlineData("from=2026-11-13T00:00:00Z&to=2026-11-12T00:00:00Z")]
    [InlineData("fromDate=2026-11-12&to=2026-11-13T00:00:00Z")]
    [InlineData("fromDate=invalid")]
    [InlineData("minSystolicPressure=150&maxSystolicPressure=120")]
    [InlineData("minDiastolicPressure=100&maxDiastolicPressure=80")]
    [InlineData("minPulse=100&maxPulse=70")]
    [InlineData("minSystolicPressure=0")]
    [InlineData("maxDiastolicPressure=201")]
    [InlineData("maxPulse=301")]
    [InlineData("migraineAttackId=00000000-0000-0000-0000-000000000000")]
    [InlineData("migraineAttackId=11111111-1111-1111-1111-111111111111&hasMigraineAttack=false")]
    [InlineData("bodyPosition=99")]
    [InlineData("sortBy=unknown")]
    [InlineData("sortDirection=sideways")]
    [InlineData("hasMigraineAttack=unknown")]
    public async Task List_WithInvalidFilter_ShouldReturnBadRequest(string query)
    {
        var token = await CreateUserAndLoginAsync();
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/vital-measurements?" + query, token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_WithFilters_ShouldRequireAuthorization()
    {
        using var response = await _client.GetAsync("/api/vital-measurements?minPulse=70&sortBy=pulse");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<PagedResponse<VitalMeasurementResponse>> GetFilteredMeasurementsAsync(string token, string query)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/vital-measurements?" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<VitalMeasurementResponse>>(options);
        Assert.NotNull(result);
        return result;
    }

    private async Task<Guid> CreateFilterAttackAsync(string token)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/migraine-attacks", token,
            new { startDate = "2026-11-12", endDate = "2026-11-12", intensity = 5, painLocation = "LeftSide" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return result.GetProperty("id").GetGuid();
    }

    private async Task CreateFilterMeasurementAsync(string token, Guid? attackId, string position, string notes)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/vital-measurements", token,
            new { systolicPressure = 120, diastolicPressure = 80, pulse = 70,
                measuredAt = "2026-11-12T10:00:00Z", migraineAttackId = attackId, bodyPosition = position, notes });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<VitalMeasurementStatisticsResponse> GetStatisticsAsync(
        string token, string query = "")
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, "/api/vital-measurements/statistics" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VitalMeasurementStatisticsResponse>();
        Assert.NotNull(result);
        return result;
    }

    private static void AssertEmptyStatistics(VitalMeasurementStatisticsResponse stats)
    {
        Assert.Equal(0, stats.MeasurementCount);
        Assert.Null(stats.AverageSystolicPressure);
        Assert.Null(stats.MinSystolicPressure);
        Assert.Null(stats.MaxSystolicPressure);
        Assert.Null(stats.AverageDiastolicPressure);
        Assert.Null(stats.MinDiastolicPressure);
        Assert.Null(stats.MaxDiastolicPressure);
        Assert.Null(stats.AveragePulse);
        Assert.Null(stats.MinPulse);
        Assert.Null(stats.MaxPulse);
    }

    private async Task<VitalMeasurementResponse>
        CreateMeasurementAsync(
            string token,
            int systolic,
            int diastolic,
            int pulse,
            string notes,
            DateTimeOffset? measuredAt = null)
    {
        var request =
            new CreateVitalMeasurementRequest
            {
                SystolicPressure =
                    systolic,

                DiastolicPressure =
                    diastolic,

                Pulse =
                    pulse,

                MeasuredAt =
                    measuredAt ??
                    new DateTimeOffset(
                        2026, 11, 12,
                        10, 0, 0,
                        TimeSpan.Zero),

                Notes =
                    notes
            };

        var response =
            await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/vital-measurements",
                token,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    VitalMeasurementResponse>();

        Assert.NotNull(result);

        return result;
    }

    private async Task<string>
        CreateUserAndLoginAsync()
    {
        var email =
            $"vitals-{Guid.NewGuid():N}@example.com";

        const string password =
            "Password123";

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest
                {
                    Name =
                        "Vitals Test User",

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
                    body);
        }

        return await _client.SendAsync(
            request);
    }
}
