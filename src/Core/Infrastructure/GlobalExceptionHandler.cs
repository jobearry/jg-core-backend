using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure;

/// <summary>
/// The last line of defence: whatever escapes an endpoint — a binding failure, a DI
/// error, a bug the feature did not classify — becomes the same
/// <c>application/problem+json</c> shape every deliberate failure uses.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    /// <c>499 Client Closed Request</c>. Not an official status code and not in
    /// <see cref="StatusCodes"/>, but it is the de-facto convention (nginx) for a client
    /// that hung up before the response was ready.
    /// </summary>
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A client that navigates away is not a failure of this service. Logging it at
        // Error level would bury real defects under noise from normal navigation.
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                "Request {Method} {Path} was cancelled by the client.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Same shape, status and code as a failure that went through a feature, so a
        // client never has to special-case "the really bad 500". Exception detail goes
        // to the log, never to the response.
        await Results.Problem(
            title: "Unexpected error",
            detail: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?> { ["code"] = "unexpected_error" })
            .ExecuteAsync(httpContext);

        return true;
    }
}
