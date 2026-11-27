using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task Export_EmptyArchive_ShouldContainHeadersAndUtf8Bom()
    {
        var token = await CreateUserAndLoginAsync();
        var csv = await ReadExportAsync(token);
        Assert.Equal("\"Id\";\"StartDate\";\"EndDate\";\"StartAtUtc\";\"EndAtUtc\";\"DurationMinutes\";\"Intensity\";\"PainLocation\";\"PainLocationDescription\";\"Notes\";\"Symptoms\";\"Triggers\";\"Medications\";\"CreatedAtUtc\";\"DurationDays\"\r\n", csv.Attacks);
        Assert.Equal("\"Id\";\"SystolicPressure\";\"DiastolicPressure\";\"Pulse\";\"MeasuredAtUtc\";\"Notes\";\"CreatedAtUtc\";\"MigraineAttackId\";\"BodyPosition\"\r\n", csv.Vitals);
    }

    [Fact]
    public async Task Export_ShouldIncludeAllMedicationRatingsAndLinkedMeasurementFields()
    {
        var token = await CreateUserAndLoginAsync();
        var medication = await PostForIdAsync(token, "/api/medications", new { name = "Export medicine" });
        var attack = await PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = "2026-11-12", endDate = "2026-11-13",
            startAt = "2026-11-12T23:00:00Z", endAt = "2026-11-13T01:00:00Z",
            intensity = 5, painLocation = "LeftSide",
            medications = new[]
            {
                new { medicationId = medication, dosage = "dose1", takenAt = "2026-11-12T23:01:00Z", effectiveness = "Helped" },
                new { medicationId = medication, dosage = "dose2", takenAt = "2026-11-12T23:02:00Z", effectiveness = "PartiallyHelped" },
                new { medicationId = medication, dosage = "dose3", takenAt = "2026-11-12T23:03:00Z", effectiveness = "DidNotHelp" },
                new { medicationId = medication, dosage = "dose4", takenAt = "2026-11-12T23:04:00Z", effectiveness = "NotAssessed" }
            }
        });
        var linked = await PostVitalAsync(token, "linked", "2026-11-12T23:10:00Z", attack, "Sitting");
        var unlinked = await PostVitalAsync(token, "unlinked", "2026-11-12T23:20:00Z");
        var csv = await ReadExportAsync(token);
        var row = FindRow(csv.Attacks, attack);
        Assert.Contains("\"120\";\"5\"", row);
        Assert.EndsWith(";\"2\"", row);
        Assert.Contains("Export medicine [dose1, 2026-11-12T23:01:00.0000000+00:00, Effectiveness=Helped]", row);
        Assert.Contains("Effectiveness=PartiallyHelped]", row);
        Assert.Contains("Effectiveness=DidNotHelp]", row);
        Assert.Contains("Effectiveness=NotAssessed]", row);
        Assert.EndsWith($";\"{attack}\";\"Sitting\"", FindRow(csv.Vitals, linked));
        Assert.EndsWith(";\"\";\"\"", FindRow(csv.Vitals, unlinked));
    }

    [Fact]
    public async Task Export_ShouldDistinguishUnknownAndZeroDuration()
    {
        var token = await CreateUserAndLoginAsync();
        var unknown = await PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = "2026-11-12", endDate = "2026-11-14", intensity = 5, painLocation = "LeftSide"
        });
        var partial = await PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = "2026-11-12", endDate = "2026-11-13", startAt = "2026-11-12T09:00:00Z", intensity = 5, painLocation = "LeftSide"
        });
        var zero = await PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = "2026-11-12", endDate = "2026-11-12", startAt = "2026-11-12T10:00:00Z", endAt = "2026-11-12T10:00:00Z", intensity = 5, painLocation = "LeftSide"
        });
        var csv = await ReadExportAsync(token);
        var unknownRow = FindRow(csv.Attacks, unknown).Split(';');
        Assert.Equal("\"\"", unknownRow[3]);
        Assert.Equal("\"\"", unknownRow[4]);
        Assert.Equal("\"\"", unknownRow[5]);
        Assert.Equal("\"3\"", unknownRow[^1]);
        var partialRow = FindRow(csv.Attacks, partial).Split(';');
        Assert.NotEqual("\"\"", partialRow[3]);
        Assert.Equal("\"\"", partialRow[4]);
        Assert.Equal("\"\"", partialRow[5]);
        Assert.Equal("\"0\"", FindRow(csv.Attacks, zero).Split(';')[5]);
    }

    [Fact]
    public async Task Export_CalendarPeriod_ShouldIncludeOverlapsAndEntireLastUtcDay()
    {
        var token = await CreateUserAndLoginAsync();
        var overlap = await PostCalendarAttackAsync(token, "2026-11-11", "2026-11-12");
        var outside = await PostCalendarAttackAsync(token, "2026-11-13", "2026-11-14");
        var first = await PostVitalAsync(token, "first", "2026-11-12T00:00:00Z");
        var last = await PostVitalAsync(token, "last", "2026-11-12T23:59:59.999Z");
        var before = await PostVitalAsync(token, "before", "2026-11-11T23:59:59Z");
        var after = await PostVitalAsync(token, "after", "2026-11-13T00:00:00Z");
        var csv = await ReadExportAsync(token, "?fromDate=2026-11-12&toDate=2026-11-12");
        Assert.Contains(overlap.ToString(), csv.Attacks);
        Assert.DoesNotContain(outside.ToString(), csv.Attacks);
        Assert.Contains(first.ToString(), csv.Vitals);
        Assert.Contains(last.ToString(), csv.Vitals);
        Assert.DoesNotContain(before.ToString(), csv.Vitals);
        Assert.DoesNotContain(after.ToString(), csv.Vitals);
    }

    [Theory]
    [InlineData("?fromDate=2026-11-12", false, true)]
    [InlineData("?toDate=2026-11-11", true, false)]
    public async Task Export_OneSidedCalendarPeriod_ShouldFilterBothFiles(string query, bool earlier, bool later)
    {
        var token = await CreateUserAndLoginAsync();
        var a = await PostCalendarAttackAsync(token, "2026-11-11", "2026-11-11");
        var b = await PostCalendarAttackAsync(token, "2026-11-12", "2026-11-12");
        var v1 = await PostVitalAsync(token, "earlier", "2026-11-11T12:00:00Z");
        var v2 = await PostVitalAsync(token, "later", "2026-11-12T12:00:00Z");
        var csv = await ReadExportAsync(token, query);
        Assert.Equal(earlier, csv.Attacks.Contains(a.ToString()));
        Assert.Equal(later, csv.Attacks.Contains(b.ToString()));
        Assert.Equal(earlier, csv.Vitals.Contains(v1.ToString()));
        Assert.Equal(later, csv.Vitals.Contains(v2.ToString()));
    }

    [Fact]
    public async Task Export_LegacyPeriod_ShouldKeepLocalAttackDatesAndExactMeasurementTimes()
    {
        var token = await CreateUserAndLoginAsync();
        var localDay = await PostCalendarAttackAsync(token, "2026-11-12", "2026-11-12");
        var priorDay = await PostCalendarAttackAsync(token, "2026-11-11", "2026-11-11");
        var exact = await PostVitalAsync(token, "exact", "2026-11-11T21:00:00Z");
        var after = await PostVitalAsync(token, "after", "2026-11-11T21:00:01Z");
        var csv = await ReadExportAsync(token, "?from=2026-11-12T00:00:00%2B03:00&to=2026-11-12T00:00:00%2B03:00");
        Assert.Contains(localDay.ToString(), csv.Attacks);
        Assert.DoesNotContain(priorDay.ToString(), csv.Attacks);
        Assert.Contains(exact.ToString(), csv.Vitals);
        Assert.DoesNotContain(after.ToString(), csv.Vitals);
    }

    [Theory]
    [InlineData("=1+1", "=1+1")]
    [InlineData("+1+1", "+1+1")]
    [InlineData("-1+1", "-1+1")]
    [InlineData("@SUM(1)", "@SUM(1)")]
    [InlineData("  =1+1", "=1+1")]
    [InlineData("\uFF1D1+1", "\uFF1D1+1")]
    public async Task Export_ShouldNeutralizeFormulaLikeText(string input, string normalized)
    {
        var token = await CreateUserAndLoginAsync();
        var attack = await PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = "2026-11-12", endDate = "2026-11-12", intensity = 5,
            painLocation = "Other", painLocationDescription = input, notes = input
        });
        var vital = await PostVitalAsync(token, input, "2026-11-12T12:00:00Z");
        var csv = await ReadExportAsync(token);
        Assert.Contains($"\"'{normalized}\";\"'{normalized}\"", FindRow(csv.Attacks, attack));
        Assert.Contains($"\"'{normalized}\"", FindRow(csv.Vitals, vital));
    }

    [Fact]
    public async Task Export_ShouldEscapeQuotesAndDelimitersAndPreserveCyrillic()
    {
        var token = await CreateUserAndLoginAsync();
        var vital = await PostVitalAsync(token, "Давление; \"замер\"\r\nповтор", "2026-11-12T12:00:00Z");
        var csv = await ReadExportAsync(token);
        Assert.Contains("\"Давление; \"\"замер\"\" повтор\"", FindRow(csv.Vitals, vital));
        Assert.Equal(2, csv.Vitals.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Theory]
    [InlineData("?fromDate=2026-11-13&toDate=2026-11-12")]
    [InlineData("?fromDate=2026-11-12&to=2026-11-13T00:00:00Z")]
    [InlineData("?from=2026-11-12T00:00:00Z&toDate=2026-11-13")]
    [InlineData("?fromDate=invalid")]
    [InlineData("?toDate=invalid")]
    public async Task Export_InvalidCalendarPeriod_ShouldReturnBadRequest(string query)
    {
        var token = await CreateUserAndLoginAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/data-export" + query, token);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_ShouldExcludeForeignAttacksAndLinkedVitals()
    {
        var token = await CreateUserAndLoginAsync();
        var other = await CreateUserAndLoginAsync();
        var ownAttack = await PostCalendarAttackAsync(token, "2026-11-12", "2026-11-12");
        var foreignAttack = await PostCalendarAttackAsync(other, "2026-11-12", "2026-11-12");
        var ownVital = await PostVitalAsync(token, "own", "2026-11-12T12:00:00Z", ownAttack, "Standing");
        var foreignVital = await PostVitalAsync(other, "foreign", "2026-11-12T12:00:00Z", foreignAttack, "Lying");
        var csv = await ReadExportAsync(token, "?fromDate=2026-11-12&toDate=2026-11-12");
        Assert.Contains(ownAttack.ToString(), csv.Attacks);
        Assert.Contains(ownVital.ToString(), csv.Vitals);
        Assert.DoesNotContain(foreignAttack.ToString(), csv.Attacks);
        Assert.DoesNotContain(foreignAttack.ToString(), csv.Vitals);
        Assert.DoesNotContain(foreignVital.ToString(), csv.Vitals);
    }

    private async Task<Guid> PostForIdAsync(string token, string path, object body)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, path, token);
        request.Content = JsonContent.Create(body);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private Task<Guid> PostCalendarAttackAsync(string token, string start, string end) =>
        PostForIdAsync(token, "/api/migraine-attacks", new
        {
            startDate = start, endDate = end, intensity = 5, painLocation = "LeftSide"
        });

    private Task<Guid> PostVitalAsync(string token, string notes, string measuredAt,
        Guid? attackId = null, string? bodyPosition = null) =>
        PostForIdAsync(token, "/api/vital-measurements", new
        {
            systolicPressure = 120, diastolicPressure = 80, pulse = 70,
            measuredAt, notes, migraineAttackId = attackId, bodyPosition
        });

    private async Task<(string Attacks, string Vitals)> ReadExportAsync(string token, string query = "")
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/data-export" + query, token);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
        return (await ReadCsvEntryAsync(archive, "migraine-attacks.csv"),
            await ReadCsvEntryAsync(archive, "vital-measurements.csv"));
    }

    private static async Task<string> ReadCsvEntryAsync(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        Assert.True(bytes.Length >= 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    private static string FindRow(string csv, Guid id) => Assert.Single(csv
        .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
        .Where(row => row.StartsWith($"\"{id}\";", StringComparison.Ordinal)));

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