using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Portfolio.Domain.ValueObjects;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract for the admin Recommendation writes. Public reads stay anonymous;
/// every write needs the Admin service token and uses the admin rate-limit policy.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class RecommendationAdminEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private const string CreatePath = "/api/v1/admin/recommendation";

    private static readonly DateTime SeededPostedAt =
        new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    private readonly JGBackendApiFactory _factory;

    public RecommendationAdminEndpointsTests(JGBackendApiFactory factory)
    {
        _factory = factory;
        Seed(factory);
    }

    [Fact]
    public async Task AdminCreate_WithToken_Returns201AndLocation()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(CreatePath, new
        {
            personName = "Jane Doe",
            description = "Excellent teammate",
            careerPosition = "Staff Engineer"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/recommendation/2", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Jane Doe", body.GetProperty("personName").GetString());
        Assert.Equal("Staff Engineer", body.GetProperty("careerPosition").GetString());
        Assert.Equal(2, _factory.Recommendations.Recommendations.Count);
    }

    [Fact]
    public async Task AdminCreate_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(CreatePath, new
        {
            personName = "Jane Doe",
            description = "Excellent teammate",
            careerPosition = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreate_WithBlankPersonName_Returns400ValidationFailed()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(CreatePath, new
        {
            personName = "   ",
            description = "Excellent teammate",
            careerPosition = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminCreate_WithNullBody_Returns400ValidationFailed()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            CreatePath, new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminUpdate_WithToken_Returns200AndKeepsPostedAt()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{CreatePath}/1", new
        {
            personName = "Updated Name",
            description = "Updated description",
            careerPosition = (string?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Name", body.GetProperty("personName").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("careerPosition").ValueKind);
        Assert.Equal(1, body.GetProperty("id").GetInt32());
        Assert.Equal(SeededPostedAt, body.GetProperty("postedAt").GetDateTime());
        Assert.Equal(SeededPostedAt, Assert.Single(_factory.Recommendations.Recommendations).PostedAt);
    }

    [Fact]
    public async Task AdminUpdate_ForMissingId_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsJsonAsync($"{CreatePath}/99", new
        {
            personName = "Updated Name",
            description = "Updated description",
            careerPosition = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdate_WithNullBody_Returns400ValidationFailed()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PutAsync(
            $"{CreatePath}/1", new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminDelete_WithToken_Returns204()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{CreatePath}/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_factory.Recommendations.Recommendations);
    }

    [Fact]
    public async Task AdminDelete_ForMissingId_Returns404()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync($"{CreatePath}/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDelete_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{CreatePath}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.Recommendations.Recommendations.Clear();
        factory.Recommendations.Recommendations.Add(new DomainRecommendation(
            1, "Atsushi Deguchi", "Great to work with", new Role("Engineer"), SeededPostedAt));
    }
}
