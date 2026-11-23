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
