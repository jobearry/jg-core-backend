using Portfolio.ErrorHandling;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Api.Tests;

/// <summary>
/// Host wiring that is not owned by a feature slice.
/// </summary>
public class PortfolioEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;

    public PortfolioEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Easy to forget and invisible when missing: without this registration
    /// <c>UseExceptionHandler()</c> has no handler to delegate to, and every unhandled
    /// exception silently becomes a bare 500 again.
    /// </summary>
    [Fact]
    public void TheGlobalExceptionHandlerIsRegistered()
    {
        Assert.Contains(
            _factory.Services.GetServices<IExceptionHandler>(),
            handler => handler is GlobalExceptionHandler);
    }
}
