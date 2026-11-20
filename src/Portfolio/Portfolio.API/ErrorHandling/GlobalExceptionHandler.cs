using Portfolio.Application.Common;
using Portfolio.Endpoints;
using Microsoft.AspNetCore.Diagnostics;

namespace Portfolio.ErrorHandling;

/// <summary>
/// The last line of defence: whatever escapes an endpoint — a bug in model binding, a
/// DI failure, anything the services did not classify — becomes the same
/// <c>application/problem+json</c> shape every other failure already uses.
///
/// <remarks>
/// Plan 2.2 gave the services a typed error and one mapper, but an exception thrown
/// <em>outside</em> a service still produced a bare, bodyless 500. This closes that gap.
/// </remarks>
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// <c>499 Client Closed Request</c>. Not an official status code and not in
    /// <see cref="StatusCodes"/>, but it is the de-facto convention (nginx) for a client
    /// that hung up before the response was ready.
    /// </summary>
    private const int ClientClosedRequest = 499;

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A client that disconnected is not a failure of this service. Logging it at
        // Error level would bury real defects under noise from normal navigation, and
        // answering 500 would blame the server for the browser's behaviour.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request {Method} {Path} was cancelled by the client.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Same shape, status and code as a failure that went through a service, so a
        // client never has to special-case "the really bad 500".
        var error = Error.Unexpected("An unexpected error occurred.");

        await Results.Problem(
            title: "Unexpected error",
            detail: error.Message,
            statusCode: ApiResult.StatusCodeFor(error.Kind),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code })
            .ExecuteAsync(httpContext);

        return true;
    }
}
