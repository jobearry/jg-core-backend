using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract for Experience admin writes. Reads stay anonymous; every write needs
/// the Admin token. Input that violates a value-object invariant is 400, not 500.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class ExperienceAdminEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private const string ExperiencePath = "/api/v1/admin/experience";

    private readonly JGBackendApiFactory _factory;

    public ExperienceAdminEndpointsTests(JGBackendApiFactory factory)
    {
        _factory = factory;
        Seed(factory);
    }

    [Fact]
    public async Task AdminCreateExperience_WithToken_Returns201AndLocation()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(ExperiencePath, new
        {
            company = "Globex",
            role = "Staff Engineer",
            startedAt = new DateTime(2020, 1, 1),
            finishedAt = new DateTime(2021, 6, 30),
            description = "A role",
            responsibility = "Owned the platform",
            type = "Corporate"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/experience/5", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Globex", body.GetProperty("company").GetString());
        Assert.Equal("Staff Engineer", body.GetProperty("role").GetString());
        Assert.Empty(body.GetProperty("projects").EnumerateArray());
    }

    [Fact]
    public async Task AdminCreateExperience_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(ExperiencePath, new
        {
            company = "Globex",
            role = (string?)null,
            startedAt = (DateTime?)new DateTime(2020, 1, 1),
            finishedAt = (DateTime?)null,
            description = (string?)null,
            responsibility = (string?)null,
            type = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateExperience_WithBlankCompany_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(ExperiencePath, new
        {
            company = "  ",
            role = (string?)null,
            startedAt = (DateTime?)new DateTime(2020, 1, 1),
            finishedAt = (DateTime?)null,
            description = (string?)null,
            responsibility = (string?)null,
            type = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminCreateExperience_WithFutureStart_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(ExperiencePath, new
        {
            company = "Globex",
            role = (string?)null,
            startedAt = (DateTime?)DateTime.UtcNow.Date.AddDays(2),
            finishedAt = (DateTime?)null,
            description = (string?)null,
            responsibility = (string?)null,
            type = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateExperience_WithNullBody_Returns400()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            ExperiencePath, new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateExperience_WithToken_Returns200AndKeepsTheId()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{ExperiencePath}/4", new
        {
            company = "Initech",
            role = "Staff Engineer",
            startedAt = new DateTime(2020, 1, 1),
            finishedAt = (DateTime?)null,
            description = "New description",
            responsibility = "New responsibility",
            type = "Contract"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, body.GetProperty("id").GetInt32());
        Assert.Equal("Initech", body.GetProperty("company").GetString());
        Assert.Equal("New description", body.GetProperty("description").GetString());
    }

    [Fact]
    public async Task AdminUpdateExperience_ForMissingId_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{ExperiencePath}/99", new
        {
            company = "Initech",
            role = (string?)null,
            startedAt = (DateTime?)new DateTime(2020, 1, 1),
            finishedAt = (DateTime?)null,
            description = (string?)null,
            responsibility = (string?)null,
            type = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteExperience_WithToken_Returns204()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{ExperiencePath}/4");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.Experience.Experiences);
    }

    [Fact]
    public async Task AdminDeleteExperience_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{ExperiencePath}/4");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.Experience.Failure = null;
        factory.Experience.Experiences.Clear();
        factory.Experience.Experiences.Add(new Experience(
            4,
            new Company("Applied ICT Inc."),
            new Period(new DateTime(2020, 1, 1), null),
            new Role("Software Engineer")));
    }
}
