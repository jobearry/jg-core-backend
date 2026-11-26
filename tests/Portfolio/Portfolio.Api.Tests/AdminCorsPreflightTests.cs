using System.Net;

namespace Portfolio.Api.Tests;

/// <summary>
/// CORS metadata is only useful if the host's middleware honours it. These preflight
/// checks lock the browser contract for the planned admin app.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class AdminCorsPreflightTests : IClassFixture<JGBackendApiFactory>
{
    private const string Origin = "https://jgolimlim-portfolio.vercel.app";

    private readonly HttpClient _client;

    public AdminCorsPreflightTests(JGBackendApiFactory factory) =>
        _client = factory.CreateClient();

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task AdminRoute_AllowsPreflightForWriteMethod(string method)
    {
        using var request = Preflight("/api/v1/admin/experience/1", method);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains(Origin, origins);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods));
        Assert.Contains(
            method,
            methods.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries)));
    }

    [Fact]
    public async Task PublicRoute_DoesNotAllowDeletePreflight()
    {
        using var request = Preflight("/api/v1/experience", "DELETE");

        var response = await _client.SendAsync(request);

        Assert.False(
            response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods)
            && methods.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries))
                .Contains("DELETE"));
    }

    private static HttpRequestMessage Preflight(string path, string method)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", Origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type, authorization");
        return request;
    }
}
