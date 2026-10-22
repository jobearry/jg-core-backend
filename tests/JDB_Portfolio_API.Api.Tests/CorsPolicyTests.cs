using JDB_Portfolio_API;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace JDB_Portfolio_API.Api.Tests;

/// <summary>
/// The CORS allow-list, tested as policy rather than over HTTP: the browser is the only
/// component that enforces it, so a passing <c>curl</c> proves nothing about what a page
/// on an origin can read.
/// </summary>
public class CorsPolicyTests
{
    [Fact]
    public async Task Production_AllowsTheSpaOriginsButNotLocalhost()
    {
        var policy = await PolicyFor("Production");

        Assert.Contains("https://jgolimlim-portfolio.vercel.app", policy.Origins);
        Assert.DoesNotContain("http://localhost:4200", policy.Origins);
    }

    [Fact]
    public async Task Development_AddsTheLocalSpaOrigin()
    {
        var policy = await PolicyFor("Development");

        Assert.Contains("http://localhost:4200", policy.Origins);
    }

    /// <summary>
    /// Bearer tokens ride in the <c>Authorization</c> header, so nothing needs the
    /// credentials flag, and there is no cookie to send. Pinning that keeps a future
    /// cookie-based flow from silently inheriting cross-origin access.
    /// </summary>
    [Fact]
    public async Task ThePolicy_DoesNotAllowCredentials()
    {
        var policy = await PolicyFor("Development");

        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public async Task ThePolicy_IsAnAllowListNarrowedToTheMethodsAndHeadersTheApiUses()
    {
        var policy = await PolicyFor("Development");

        Assert.False(policy.AllowAnyOrigin);
        Assert.False(policy.AllowAnyMethod);
        Assert.False(policy.AllowAnyHeader);
        Assert.Equal(new[] { "GET", "POST" }, policy.Methods);
        Assert.Equal(new[] { "Content-Type", "Authorization" }, policy.Headers);
    }

    [Fact]
    public async Task AConfiguredOrigin_IsTrimmedOfTrailingWhitespaceAndSlash()
    {
        // The browser's Origin header never ends in a slash, so a value pasted from an
        // address bar would otherwise never match.
        var policy = await PolicyFor("Production", " https://example.com/ ");

        Assert.Contains("https://example.com", policy.Origins);
    }

    private static async Task<CorsPolicy> PolicyFor(string environmentName, string allowedOrigin = "https://jgolimlim-portfolio.vercel.app")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = allowedOrigin,
                ["Cors:DevelopmentOrigins:0"] = "http://localhost:4200"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCorsOrigins(configuration, new FakeEnvironment(environmentName));

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<ICorsPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(new DefaultHttpContext(), ConfigurationSettings.SpaCorsPolicy);

        return policy!;
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "JDB_Portfolio_API.Api.Tests";

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
