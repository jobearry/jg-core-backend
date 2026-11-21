extern alias JGBackend;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Portfolio.Api.Tests;

/// <summary>
/// The host owns the CORS allow-list now, so its policy is tested here rather than
/// through the Portfolio host.
/// </summary>
public sealed class JGBackendCorsPolicyTests
{
    [Fact]
    public async Task Production_UsesConfiguredOriginsButNotLocalhost()
    {
        var policy = await PolicyFor("Production");

        Assert.Contains("https://jgolimlim-portfolio.vercel.app", policy.Origins);
        Assert.DoesNotContain("http://localhost:4200", policy.Origins);
    }

    [Fact]
    public async Task Development_AddsTheConfiguredLocalOrigin()
    {
        var policy = await PolicyFor("Development");

        Assert.Contains("http://localhost:4200", policy.Origins);
    }

    [Fact]
    public async Task ThePolicy_DoesNotAllowCredentialsAndIsNarrowed()
    {
        var policy = await PolicyFor("Development");

        Assert.False(policy.SupportsCredentials);
        Assert.False(policy.AllowAnyOrigin);
        Assert.False(policy.AllowAnyMethod);
        Assert.False(policy.AllowAnyHeader);
        Assert.Equal(new[] { "GET", "POST" }, policy.Methods);
        Assert.Equal(new[] { "Content-Type", "Authorization" }, policy.Headers);
    }

    private static async Task<CorsPolicy> PolicyFor(
        string environmentName,
        string allowedOrigin = "https://jgolimlim-portfolio.vercel.app")
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
        JGBackend::JGBackend.Configuration.CorsConfiguration.AddCorsOrigins(
            services, configuration, new FakeEnvironment(environmentName));

        var policyProvider = services.BuildServiceProvider()
            .GetRequiredService<ICorsPolicyProvider>();

        return (await policyProvider.GetPolicyAsync(new DefaultHttpContext(), "AllowSPA"))!;
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Portfolio.Api.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
