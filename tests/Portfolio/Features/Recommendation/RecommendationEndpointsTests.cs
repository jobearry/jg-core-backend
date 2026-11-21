using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Api.Tests;

/// <summary>
/// HTTP contract tests for the Recommendation slice. An undeclared career
/// position is null on the wire, never an empty string.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public class RecommendationEndpointsTests : IClassFixture<JGBackendApiFactory>
{
    private readonly HttpClient _client;

    public RecommendationEndpointsTests(JGBackendApiFactory factory)
    {
        _client = factory.CreateClient();
        Seed(factory);
    }

    [Fact]
    public async Task GetRecommendation_FlattensCareerPositionAndAllowsUndeclared()
    {
        var response = await _client.GetAsync("/api/v1/recommendation");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var declared = FindById(json, 1);
        Assert.Equal("Engineer", declared.GetProperty("careerPosition").GetString());

        var undeclared = FindById(json, 2);
        Assert.Equal(JsonValueKind.Null, undeclared.GetProperty("careerPosition").ValueKind);
    }

    private static JsonElement FindById(JsonElement array, int id) =>
        array.EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == id);

    private static void Seed(JGBackendApiFactory factory)
    {
        factory.Recommendations.Recommendations.Clear();
        factory.Recommendations.Recommendations.Add(new Recommendation(
            1, "Atsushi Deguchi", "Great to work with", new Role("Engineer"),
            new DateTime(2026, 6, 27)));
        factory.Recommendations.Recommendations.Add(new Recommendation(
            2, "Kentaro Komiya", "Grateful for the hard work", null,
            new DateTime(2026, 6, 27)));
    }
}
