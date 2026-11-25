using JGBackend.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Portfolio.Api.Tests;

/// <summary>
/// CORS belongs to the composed host, so its policy is tested here rather than
/// through a feature slice.
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

    [Fact]
    public async Task AdminPolicy_AllowsWriteMethodsAndSharesOrigins()
    {
        var spa = await PolicyFor("Production");
        var admin = await PolicyFor("Production", policyName: CorsConfiguration.AdminCorsPolicy);

        Assert.Equal(spa.Origins, admin.Origins);
        Assert.Equal(spa.Headers, admin.Headers);
        Assert.Equal(new[] { "GET", "POST", "PUT", "DELETE" }, admin.Methods);
        Assert.False(admin.SupportsCredentials);
        Assert.False(admin.AllowAnyOrigin);
        Assert.False(admin.AllowAnyMethod);
        Assert.False(admin.AllowAnyHeader);
    }

    [Fact]
    public async Task AConfiguredOrigin_IsTrimmedOfTrailingWhitespaceAndSlash()
    {
        // The browser's Origin header never ends in a slash, so a value pasted from an
        // address bar would otherwise never match.
        var policy = await PolicyFor("Production", " https://example.com/ ");

        Assert.Contains("https://example.com", policy.Origins);
    }

    private static async Task<CorsPolicy> PolicyFor(
        string environmentName,
        string allowedOrigin = "https://jgolimlim-portfolio.vercel.app",
        string policyName = "AllowSPA")
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
        CorsConfiguration.AddCorsOrigins(
            services, configuration, new FakeEnvironment(environmentName));

        var policyProvider = services.BuildServiceProvider()
            .GetRequiredService<ICorsPolicyProvider>();

        return (await policyProvider.GetPolicyAsync(new DefaultHttpContext(), policyName))!;
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
