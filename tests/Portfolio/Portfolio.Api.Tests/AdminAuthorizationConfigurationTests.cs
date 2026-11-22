using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Portfolio.Api.Tests;

/// <summary>
/// The admin policy and its fallback are security boundaries, not implementation
/// details. They are pinned here so a future config change cannot silently drop
/// either one.
/// </summary>
[Collection(JGBackendHostCollection.Name)]
public sealed class AdminAuthorizationConfigurationTests : IClassFixture<JGBackendApiFactory>
{
    private readonly JGBackendApiFactory _factory;

    public AdminAuthorizationConfigurationTests(JGBackendApiFactory factory) =>
        _factory = factory;

    [Fact]
    public void Authorization_DefinesAnAdminPolicyRequiringTheAdminRole()
    {
        var options = _factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = options.GetPolicy("Admin");

        Assert.NotNull(policy);
        AssertAdminRoleRequirement(policy!);
    }

    [Fact]
    public void Authorization_FallbackPolicyRequiresTheAdminRole()
    {
        var options = _factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        AssertAdminRoleRequirement(options.FallbackPolicy!);
    }

    private static void AssertAdminRoleRequirement(AuthorizationPolicy policy) =>
        Assert.Contains(policy.Requirements, requirement =>
            requirement is ClaimsAuthorizationRequirement claim &&
            claim.ClaimType == "role" &&
            claim.AllowedValues is not null &&
            claim.AllowedValues.Contains("Admin"));
}
