using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;

namespace Portfolio.Application.Tests;

/// <summary>
/// Plan 2.2 — the exception → <see cref="ErrorKind"/> mapping. This is where a status
/// code is decided (indirectly), so it is worth testing directly rather than only
/// through a service.
/// </summary>
public class ResultErrorsTests
{
    [Fact]
    public void From_WithADatabaseFailure_IsInfrastructure()
    {
        var error = ResultErrors.From(new FakeDbException("timeout"), "load the data");

        Assert.Equal(ErrorKind.Infrastructure, error.Kind);
        Assert.Equal("database_unavailable", error.Code);
    }

    [Fact]
    public void From_WithAWrappedDatabaseFailure_IsInfrastructure()
    {
        var wrapped = new InvalidOperationException("SaveChanges failed", new FakeDbException("timeout"));

        var error = ResultErrors.From(wrapped, "load the data");

        Assert.Equal(ErrorKind.Infrastructure, error.Kind);
        Assert.Equal("database_unavailable", error.Code);
    }

    [Fact]
    public void From_WithAnHttpFailure_IsUpstream()
    {
        var error = ResultErrors.From(new HttpRequestException("no route"), "load the data");

        Assert.Equal(ErrorKind.Upstream, error.Kind);
    }

    [Fact]
    public void From_WithABug_IsUnexpectedNotInfrastructure()
    {
        var error = ResultErrors.From(new NullReferenceException(), "load the data");

        Assert.Equal(ErrorKind.Unexpected, error.Kind);
        Assert.Equal("unexpected_error", error.Code);
    }

    /// <summary>
    /// Both Notion failure modes are 502, but they are distinguishable by code — "the
    /// service is down" and "the response changed shape" need different reactions.
    /// </summary>
    [Fact]
    public void FromExternalService_WithAFailedRequest_ReportsNotionUnavailable()
    {
        var error = ResultErrors.FromExternalService(new HttpRequestException("503"), "query the page");

        Assert.Equal(ErrorKind.Upstream, error.Kind);
        Assert.Equal("notion_unavailable", error.Code);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(System.Text.Json.JsonException))]
    public void FromExternalService_WithAnUnreadableResponse_ReportsAnUpstreamFailure(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        var error = ResultErrors.FromExternalService(exception, "query the page");

        Assert.Equal(ErrorKind.Upstream, error.Kind);
        Assert.Equal("notion_invalid_response", error.Code);
    }

    [Fact]
    public void From_NeverPutsTheExceptionMessageOnTheWire()
    {
        var error = ResultErrors.From(
            new FakeDbException("secret-value-that-must-not-be-logged"),
            "load the data");

        Assert.DoesNotContain("secret-value-that-must-not-be-logged", error.Message);
    }
}
