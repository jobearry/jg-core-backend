extern alias JGBackend;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Portfolio.Api.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class JGBackendHostCollection
{
    public const string Name = "JGBackend host";
}

/// <summary>
/// Boots the composed host. Environment variables are used because the
/// top-level <c>Program</c> reads JWT configuration while the service collection
/// is being built, before a test host can add later configuration sources.
/// </summary>
public class JGBackendApiFactory : WebApplicationFactory<JGBackend::Program>
{
    public const string SigningKey = "host-tests-signing-key-that-is-long-enough";

    private static readonly Dictionary<string, string> Settings = new()
    {
        ["ConnectionStrings__JDBConnection"] =
            "Host=unused;Database=unused;Username=unused;Password=unused",
        ["Database__AutoMigrate"] = "false",
        ["Jwt__Key"] = SigningKey,
        ["Jwt__Issuer"] = "host-composition-tests",
        ["Jwt__Audience"] = "host-composition-tests",
        ["Jwt__ExpirationInMinutes"] = "60",
        ["Swagger__Enabled"] = "true",
        ["Github__BaseUrl"] = "https://api.github.com",
        ["Notion__Version"] = "2026-03-11"
    };

    private readonly Dictionary<string, string?> _originals;

    public JGBackendApiFactory()
    {
        _originals = Settings.Keys.ToDictionary(
            key => key,
            key => Environment.GetEnvironmentVariable(key));

        foreach (var setting in Settings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(Environments.Production);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var original in _originals)
            {
                Environment.SetEnvironmentVariable(original.Key, original.Value);
            }
        }

        base.Dispose(disposing);
    }
}
