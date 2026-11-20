using Portfolio.Api.Tests.Fakes;
using Portfolio.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Portfolio.Api.Tests;

/// <summary>
/// Plan 2.4 — the global exception handler. Before this, an exception thrown outside a
/// service produced a bare, bodyless 500; the shape and status are now the same as every
/// deliberate failure.
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithAnUnexpectedException_ReturnsA500ProblemDetails()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var (context, body) = ARequest();

        var handled = await handler.TryHandleAsync(
            context, new NullReferenceException("Object reference not set"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var problem = await ReadAsync(body);
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.Equal("unexpected_error", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));

        // The exception text is logged, not published.
        Assert.DoesNotContain("Object reference not set", problem.ToString());
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<NullReferenceException>(entry.Exception);
    }

    /// <summary>
    /// A client that navigates away is not a server failure. Logging it as an error would
    /// bury real defects under normal browsing noise, and answering 500 would blame the
    /// server for the browser's behaviour.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_WhenTheClientDisconnected_Returns499WithoutAnErrorLog()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var (context, _) = ARequest();

        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        context.RequestAborted = aborted.Token;

        var handled = await handler.TryHandleAsync(
            context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(LogLevel.Debug, Assert.Single(logger.Entries).Level);
    }

    /// <summary>
    /// A cancellation that did <b>not</b> come from the client — a server-side timeout, or
    /// a token bug — is still a failure. Treating every
    /// <see cref="OperationCanceledException"/> as a disconnect would make those invisible.
    /// </summary>
    [Fact]
    public async Task TryHandleAsync_WhenCancellationDidNotComeFromTheClient_ReturnsA500()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var (context, _) = ARequest();

        // RequestAborted is not signalled, so this cancellation came from somewhere else.
        await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
    }

    private static (DefaultHttpContext Context, MemoryStream Body) ARequest()
    {
        var body = new MemoryStream();

        // Results.Problem(...).ExecuteAsync resolves IProblemDetailsService from
        // RequestServices, which the real pipeline always provides.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/experience";
        context.Response.Body = body;
        return (context, body);
    }

    private static async Task<JsonElement> ReadAsync(MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<JsonElement>(body);
    }
}
