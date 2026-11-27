using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class VitalMeasurementAttackLinkApiTests
{
    private readonly HttpClient _client;
    private const string Vitals = "/api/vital-measurements";
    private const string Attacks = "/api/migraine-attacks";

    public VitalMeasurementAttackLinkApiTests(ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Create_WithoutOptionalFields_ShouldReturnNulls()
    {
        var token = await LoginNewUserAsync();
        var measurement = await CreateMeasurementAsync(token, MeasurementBody());
        Assert.Null(OptionalGuid(measurement, "migraineAttackId"));
        Assert.Null(OptionalString(measurement, "bodyPosition"));
        var stored = await GetMeasurementAsync(token, Id(measurement));
        Assert.Null(OptionalGuid(stored, "migraineAttackId"));
        Assert.Null(OptionalString(stored, "bodyPosition"));
    }

    [Theory]
    [InlineData("Sitting")]
    [InlineData("Standing")]
    [InlineData("Lying")]
    public async Task Create_WithBodyPosition_ShouldPersist(string position)
    {
        var token = await LoginNewUserAsync();
        var body = MeasurementBody();
        body["bodyPosition"] = position;
        var measurement = await CreateMeasurementAsync(token, body);
        Assert.Equal(position, OptionalString(measurement, "bodyPosition"));
        var stored = await GetMeasurementAsync(token, Id(measurement));
        Assert.Equal(position, OptionalString(stored, "bodyPosition"));
        Assert.Null(OptionalGuid(stored, "migraineAttackId"));
    }

    [Fact]
    public async Task Create_WithOwnAttack_ShouldAppearInDetailAndMeasurementList()
    {
        var token = await LoginNewUserAsync();
        var attack = await CreateAttackAsync(token);
        var otherAttack = await CreateAttackAsync(token);
        var first = await CreateMeasurementAsync(token, LinkedBody(attack, "Sitting"));
        var second = await CreateMeasurementAsync(token, LinkedBody(attack, "Standing"));
        await CreateMeasurementAsync(token, LinkedBody(otherAttack, "Lying"));
        await CreateMeasurementAsync(token, MeasurementBody());

        Assert.Equal<Guid?>(attack, OptionalGuid(first, "migraineAttackId"));
        var stored = await GetMeasurementAsync(token, Id(first));
        Assert.Equal<Guid?>(attack, OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Sitting", OptionalString(stored, "bodyPosition"));

        var detail = await GetAttackAsync(token, attack);
        var linked = detail.GetProperty("vitalMeasurements").EnumerateArray().ToArray();
        Assert.Equal(2, linked.Length);
        Assert.Contains(linked, item => Id(item) == Id(first) && OptionalString(item, "bodyPosition") == "Sitting");
        Assert.Contains(linked, item => Id(item) == Id(second));
        Assert.All(linked, item => Assert.Equal<Guid?>(attack, OptionalGuid(item, "migraineAttackId")));

        using var listResponse = await SendAsync(HttpMethod.Get, Vitals + "?page=1&pageSize=20", token);
        var list = await ReadAsync(listResponse, HttpStatusCode.OK);
        Assert.Contains(list.GetProperty("items").EnumerateArray(), item =>
            Id(item) == Id(first) && OptionalGuid(item, "migraineAttackId") == attack &&
            OptionalString(item, "bodyPosition") == "Sitting");

        var stranger = await LoginNewUserAsync();
        using var denied = await SendAsync(HttpMethod.Get, $"{Attacks}/{attack}", stranger);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WithForeignOrMissingAttack_ShouldReturnBadRequest(bool foreign)
    {
        var token = await LoginNewUserAsync();
        var attack = foreign ? await CreateAttackAsync(await LoginNewUserAsync()) : Guid.NewGuid();
        using var response = await SendAsync(HttpMethod.Post, Vitals, token, LinkedBody(attack, "Sitting"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var listResponse = await SendAsync(HttpMethod.Get, Vitals, token);
        var list = await ReadAsync(listResponse, HttpStatusCode.OK);
        Assert.Empty(list.GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_WithForeignOrMissingAttack_ShouldKeepOriginalMeasurement(bool foreign)
    {
        var token = await LoginNewUserAsync();
        var ownAttack = await CreateAttackAsync(token);
        var measurement = await CreateMeasurementAsync(token, LinkedBody(ownAttack, "Sitting"));
        var invalidAttack = foreign ? await CreateAttackAsync(await LoginNewUserAsync()) : Guid.NewGuid();
        var body = LinkedBody(invalidAttack, "Standing");
        body["systolicPressure"] = 135;
        using var response = await SendAsync(HttpMethod.Put, $"{Vitals}/{Id(measurement)}", token, body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await GetMeasurementAsync(token, Id(measurement));
        Assert.Equal<Guid?>(ownAttack, OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Sitting", OptionalString(stored, "bodyPosition"));
        Assert.Equal(120, stored.GetProperty("systolicPressure").GetInt32());
    }

    [Fact]
    public async Task Update_ShouldLinkUnlinkedMeasurementAndThenRelinkIt()
    {
        var token = await LoginNewUserAsync();
        var firstAttack = await CreateAttackAsync(token);
        var secondAttack = await CreateAttackAsync(token);
        var measurement = await CreateMeasurementAsync(token, MeasurementBody());
        var id = Id(measurement);
        var linked = await UpdateMeasurementAsync(token, id, LinkedBody(firstAttack, "Sitting"));
        Assert.Equal<Guid?>(firstAttack, OptionalGuid(linked, "migraineAttackId"));
        var moved = await UpdateMeasurementAsync(token, id, LinkedBody(secondAttack, "Standing"));
        Assert.Equal<Guid?>(secondAttack, OptionalGuid(moved, "migraineAttackId"));
        var stored = await GetMeasurementAsync(token, id);
        Assert.Equal<Guid?>(secondAttack, OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Standing", OptionalString(stored, "bodyPosition"));
        var oldDetail = await GetAttackAsync(token, firstAttack);
        Assert.Empty(oldDetail.GetProperty("vitalMeasurements").EnumerateArray());
        var newDetail = await GetAttackAsync(token, secondAttack);
        Assert.Equal(id, Id(Assert.Single(newDetail.GetProperty("vitalMeasurements").EnumerateArray())));
    }

    [Fact]
    public async Task Update_WithOmittedOptionalFields_ShouldPreserveBoth()
    {
        var token = await LoginNewUserAsync();
        var attack = await CreateAttackAsync(token);
        var measurement = await CreateMeasurementAsync(token, LinkedBody(attack, "Sitting"));
        var body = MeasurementBody();
        body["systolicPressure"] = 130;
        var updated = await UpdateMeasurementAsync(token, Id(measurement), body);
        Assert.Equal(130, updated.GetProperty("systolicPressure").GetInt32());
        var stored = await GetMeasurementAsync(token, Id(measurement));
        Assert.Equal(130, stored.GetProperty("systolicPressure").GetInt32());
        Assert.Equal<Guid?>(attack, OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Sitting", OptionalString(stored, "bodyPosition"));
    }

    [Fact]
    public async Task Update_WithExplicitNulls_ShouldClearFieldsIndependently()
    {
        var token = await LoginNewUserAsync();
        var attack = await CreateAttackAsync(token);
        var measurement = await CreateMeasurementAsync(token, LinkedBody(attack, "Sitting"));
        var id = Id(measurement);
        var body = MeasurementBody();
        body["migraineAttackId"] = null;
        await UpdateMeasurementAsync(token, id, body);
        var stored = await GetMeasurementAsync(token, id);
        Assert.Null(OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Sitting", OptionalString(stored, "bodyPosition"));
        var detail = await GetAttackAsync(token, attack);
        Assert.Empty(detail.GetProperty("vitalMeasurements").EnumerateArray());

        body = MeasurementBody();
        body["migraineAttackId"] = attack;
        await UpdateMeasurementAsync(token, id, body);
        body = MeasurementBody();
        body["bodyPosition"] = null;
        await UpdateMeasurementAsync(token, id, body);
        stored = await GetMeasurementAsync(token, id);
        Assert.Equal<Guid?>(attack, OptionalGuid(stored, "migraineAttackId"));
        Assert.Null(OptionalString(stored, "bodyPosition"));
    }

    [Fact]
    public async Task DeleteAttack_ShouldKeepMeasurementAndClearLink()
    {
        var token = await LoginNewUserAsync();
        var attack = await CreateAttackAsync(token);
        var measurement = await CreateMeasurementAsync(token, LinkedBody(attack, "Lying"));
        using var deleted = await SendAsync(HttpMethod.Delete, $"{Attacks}/{attack}", token);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var attackResponse = await SendAsync(HttpMethod.Get, $"{Attacks}/{attack}", token);
        Assert.Equal(HttpStatusCode.NotFound, attackResponse.StatusCode);
        var stored = await GetMeasurementAsync(token, Id(measurement));
        Assert.Null(OptionalGuid(stored, "migraineAttackId"));
        Assert.Equal("Lying", OptionalString(stored, "bodyPosition"));
        Assert.Equal(120, stored.GetProperty("systolicPressure").GetInt32());
        Assert.Equal(80, stored.GetProperty("diastolicPressure").GetInt32());
        Assert.Equal(70, stored.GetProperty("pulse").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidBodyPosition_ShouldReturnBadRequest(bool update)
    {
        var token = await LoginNewUserAsync();
        var body = MeasurementBody();
        body["bodyPosition"] = "NotAValidPosition";
        var uri = Vitals;
        if (update)
        {
            var measurement = await CreateMeasurementAsync(token, MeasurementBody());
            uri += $"/{Id(measurement)}";
        }
        using var response = await SendAsync(update ? HttpMethod.Put : HttpMethod.Post, uri, token, body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Dictionary<string, object?> MeasurementBody() => new()
    {
        ["systolicPressure"] = 120,
        ["diastolicPressure"] = 80,
        ["pulse"] = 70,
        ["measuredAt"] = "2026-11-12T10:00:00Z",
        ["notes"] = "Integration test measurement"
    };

    private static Dictionary<string, object?> LinkedBody(Guid attack, string position)
    {
        var body = MeasurementBody();
        body["migraineAttackId"] = attack;
        body["bodyPosition"] = position;
        return body;
    }

    private async Task<Guid> CreateAttackAsync(string token)
    {
        using var response = await SendAsync(HttpMethod.Post, Attacks, token, new
        {
            startDate = "2026-11-12",
            endDate = "2026-11-12",
            intensity = 5,
            painLocation = "LeftSide"
        });
        return Id(await ReadAsync(response, HttpStatusCode.Created));
    }

    private async Task<JsonElement> CreateMeasurementAsync(string token, object body)
    {
        using var response = await SendAsync(HttpMethod.Post, Vitals, token, body);
        return await ReadAsync(response, HttpStatusCode.Created);
    }

    private async Task<JsonElement> UpdateMeasurementAsync(string token, Guid id, object body)
    {
        using var response = await SendAsync(HttpMethod.Put, $"{Vitals}/{id}", token, body);
        return await ReadAsync(response, HttpStatusCode.OK);
    }

    private async Task<JsonElement> GetMeasurementAsync(string token, Guid id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{Vitals}/{id}", token);
        return await ReadAsync(response, HttpStatusCode.OK);
    }

    private async Task<JsonElement> GetAttackAsync(string token, Guid id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{Attacks}/{id}", token);
        return await ReadAsync(response, HttpStatusCode.OK);
    }

    private async Task<string> LoginNewUserAsync()
    {
        var email = $"vital-link-{Guid.NewGuid():N}@example.com";
        const string password = "Password123";
        using var registered = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Name = "Vital Link Test User",
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var loggedIn = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        var login = await ReadAsync(loggedIn, HttpStatusCode.OK);
        return login.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {content}");
        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }

    private static Guid Id(JsonElement item) => item.GetProperty("id").GetGuid();

    private static Guid? OptionalGuid(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetGuid() : null;

    private static string? OptionalString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
}
