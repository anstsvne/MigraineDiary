using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MigraineDiary.Api.Contracts.Auth;
using MigraineDiary.Api.Tests.Infrastructure;
using Xunit;

namespace MigraineDiary.Api.Tests;

[Collection("API integration tests")]
public sealed class MedicationEffectivenessApiTests
{
    private readonly HttpClient _client;
    private const string Attacks = "/api/migraine-attacks";

    public MedicationEffectivenessApiTests(ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Theory]
    [InlineData(null, "NotAssessed")]
    [InlineData("NotAssessed", "NotAssessed")]
    [InlineData("Helped", "Helped")]
    [InlineData("PartiallyHelped", "PartiallyHelped")]
    [InlineData("DidNotHelp", "DidNotHelp")]
    public async Task Create_ShouldPersistEffectiveness(string? input, string expected)
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, input));
        var intake = Assert.Single(Intakes(attack));
        Assert.Equal(expected, Effectiveness(intake));
        var stored = Assert.Single(Intakes(await GetAttackAsync(token, Id(attack))));
        Assert.Equal(Id(intake), Id(stored));
        Assert.Equal(expected, Effectiveness(stored));
        using var response = await SendAsync(HttpMethod.Get, Attacks + "?page=1&pageSize=20", token);
        var list = await ReadAsync(response, HttpStatusCode.OK);
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(Id(attack), Id(item));
        Assert.Equal(expected, Effectiveness(Assert.Single(Intakes(item))));
    }

    [Fact]
    public async Task Update_ShouldChangeAndResetEffectivenessWithoutChangingId()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        var updated = await UpdateAttackAsync(token, Id(attack), Intake(medication, "DidNotHelp", id));
        AssertIntake(Assert.Single(Intakes(updated)), id, "DidNotHelp");
        var reset = await UpdateAttackAsync(token, Id(attack), Intake(medication, "NotAssessed", id));
        AssertIntake(Assert.Single(Intakes(reset)), id, "NotAssessed");
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), id, "NotAssessed");
    }

    [Fact]
    public async Task Update_WithIdAndWithoutEffectiveness_ShouldPreserveRatingWhenDosageChanges()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        await UpdateAttackAsync(token, Id(attack), Intake(medication, id: id, dosage: "200 mg"));
        var stored = Assert.Single(Intakes(await GetAttackAsync(token, Id(attack))));
        AssertIntake(stored, id, "Helped");
        Assert.Equal("200 mg", stored.GetProperty("dosage").GetString());
    }

    [Fact]
    public async Task Update_LegacyUnchangedIntake_ShouldPreserveRatingAndId()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "PartiallyHelped"));
        var id = Id(Assert.Single(Intakes(attack)));
        await UpdateAttackAsync(token, Id(attack), Intake(medication));
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), id, "PartiallyHelped");
    }

    [Fact]
    public async Task RepeatedMedication_ShouldKeepIndependentRatingsAfterReordering()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"), Intake(medication, "DidNotHelp"));
        var entries = Intakes(attack);
        Assert.Equal(2, entries.Length);
        var firstId = Id(Assert.Single(entries.Where(x => Effectiveness(x) == "Helped")));
        var secondId = Id(Assert.Single(entries.Where(x => Effectiveness(x) == "DidNotHelp")));
        Assert.NotEqual(firstId, secondId);
        await UpdateAttackAsync(token, Id(attack), Intake(medication, id: secondId), Intake(medication, "PartiallyHelped", firstId));
        var stored = Intakes(await GetAttackAsync(token, Id(attack)));
        Assert.Equal(2, stored.Length);
        AssertIntake(Assert.Single(stored.Where(x => Id(x) == firstId)), firstId, "PartiallyHelped");
        AssertIntake(Assert.Single(stored.Where(x => Id(x) == secondId)), secondId, "DidNotHelp");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task InvalidEffectiveness_ShouldReturnBadRequest(bool update, bool numeric)
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var body = Intake(medication);
        body["effectiveness"] = numeric ? (object)999 : "UnknownRating";
        if (!update)
        {
            using var response = await SendAsync(HttpMethod.Post, Attacks, token, AttackBody(body));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        body["id"] = id;
        using var rejected = await SendAsync(HttpMethod.Put, $"{Attacks}/{Id(attack)}", token, AttackBody(body));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), id, "Helped");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_WithIntakeFromAnotherAttack_ShouldReject(bool differentUser)
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var ownerToken = differentUser ? await LoginNewUserAsync() : token;
        var otherMedication = differentUser ? await CreateMedicationAsync(ownerToken) : medication;
        var otherAttack = await CreateAttackAsync(ownerToken, Intake(otherMedication, "DidNotHelp"));
        var foreignId = Id(Assert.Single(Intakes(otherAttack)));
        using var response = await SendAsync(HttpMethod.Put, $"{Attacks}/{Id(attack)}", token,
            AttackBody(Intake(medication, "NotAssessed", foreignId)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), Id(Assert.Single(Intakes(attack))), "Helped");
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(ownerToken, Id(otherAttack)))), foreignId, "DidNotHelp");
    }

    [Fact]
    public async Task Update_WithDuplicateIntakeId_ShouldReject()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        using var response = await SendAsync(HttpMethod.Put, $"{Attacks}/{Id(attack)}", token,
            AttackBody(Intake(medication, "DidNotHelp", id), Intake(medication, "Helped", id)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), id, "Helped");
    }

    [Fact]
    public async Task Create_WithExistingIntakeId_ShouldReject()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        using var response = await SendAsync(HttpMethod.Post, Attacks, token, AttackBody(Intake(medication, "DidNotHelp", id)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertIntake(Assert.Single(Intakes(await GetAttackAsync(token, Id(attack)))), id, "Helped");
    }

    [Fact]
    public async Task Update_ShouldAddNewAndRemoveOmittedIntakes()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"), Intake(medication, "DidNotHelp", dosage: "100 mg"));
        var entries = Intakes(attack);
        var keep = Id(Assert.Single(entries.Where(x => Effectiveness(x) == "Helped")));
        var remove = Id(Assert.Single(entries.Where(x => Effectiveness(x) == "DidNotHelp")));
        await UpdateAttackAsync(token, Id(attack), Intake(medication, id: keep), Intake(medication, dosage: "300 mg"));
        var stored = Intakes(await GetAttackAsync(token, Id(attack)));
        Assert.Equal(2, stored.Length);
        AssertIntake(Assert.Single(stored.Where(x => Id(x) == keep)), keep, "Helped");
        Assert.DoesNotContain(stored, x => Id(x) == remove);
        var added = Assert.Single(stored.Where(x => Id(x) != keep));
        Assert.Equal("NotAssessed", Effectiveness(added));
        Assert.Equal("300 mg", added.GetProperty("dosage").GetString());
    }

    [Fact]
    public async Task Update_AmbiguousLegacyIntakes_ShouldRejectWithoutLosingRatings()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"), Intake(medication, "DidNotHelp"));
        using var response = await SendAsync(HttpMethod.Put, $"{Attacks}/{Id(attack)}", token, AttackBody(Intake(medication)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = Intakes(await GetAttackAsync(token, Id(attack)));
        Assert.Equal(2, stored.Length);
        foreach (var previous in Intakes(attack))
            AssertIntake(Assert.Single(stored.Where(x => Id(x) == Id(previous))), Id(previous), Effectiveness(previous));
    }

    [Fact]
    public async Task Update_WithIdAndDifferentMedication_ShouldReject()
    {
        var token = await LoginNewUserAsync();
        var medication = await CreateMedicationAsync(token);
        var replacement = await CreateMedicationAsync(token);
        var attack = await CreateAttackAsync(token, Intake(medication, "Helped"));
        var id = Id(Assert.Single(Intakes(attack)));
        using var response = await SendAsync(HttpMethod.Put, $"{Attacks}/{Id(attack)}", token, AttackBody(Intake(replacement, "DidNotHelp", id)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = Assert.Single(Intakes(await GetAttackAsync(token, Id(attack))));
        AssertIntake(stored, id, "Helped");
        Assert.Equal(medication, stored.GetProperty("medicationId").GetGuid());
    }

    private static Dictionary<string, object?> Intake(Guid medication, string? rating = null, Guid? id = null, string dosage = "400 mg")
    {
        var result = new Dictionary<string, object?>
        {
            ["medicationId"] = medication,
            ["dosage"] = dosage,
            ["takenAt"] = "2026-11-12T10:00:00Z"
        };
        if (rating is not null) result["effectiveness"] = rating;
        if (id.HasValue) result["id"] = id.Value;
        return result;
    }

    private static object AttackBody(params object[] intakes) => new
    {
        startDate = "2026-11-12", endDate = "2026-11-12", intensity = 5,
        painLocation = "LeftSide", medications = intakes
    };

    private async Task<Guid> CreateMedicationAsync(string token)
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/medications", token, new { name = $"Test medication {Guid.NewGuid():N}" });
        return Id(await ReadAsync(response, HttpStatusCode.Created));
    }

    private async Task<JsonElement> CreateAttackAsync(string token, params object[] intakes)
    {
        using var response = await SendAsync(HttpMethod.Post, Attacks, token, AttackBody(intakes));
        return await ReadAsync(response, HttpStatusCode.Created);
    }

    private async Task<JsonElement> UpdateAttackAsync(string token, Guid id, params object[] intakes)
    {
        using var response = await SendAsync(HttpMethod.Put, $"{Attacks}/{id}", token, AttackBody(intakes));
        return await ReadAsync(response, HttpStatusCode.OK);
    }

    private async Task<JsonElement> GetAttackAsync(string token, Guid id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{Attacks}/{id}", token);
        return await ReadAsync(response, HttpStatusCode.OK);
    }

    private static JsonElement[] Intakes(JsonElement attack) => attack.GetProperty("medications").EnumerateArray().ToArray();
    private static string Effectiveness(JsonElement intake) => intake.GetProperty("effectiveness").GetString()!;
    private static void AssertIntake(JsonElement intake, Guid id, string expected)
    {
        Assert.Equal(id, Id(intake));
        Assert.Equal(expected, Effectiveness(intake));
    }

    private async Task<string> LoginNewUserAsync()
    {
        var email = $"med-effect-{Guid.NewGuid():N}@example.com";
        const string password = "Password123";
        using var registered = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Name = "Medication Effectiveness Test User",
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

}
