using System;
using System.Data.Common;

namespace Portfolio.Application.Common;

/// <summary>
/// Turns an exception into a typed <see cref="Error"/>.
///
/// <para>
/// <see cref="DbException"/> is the only database type used here, and it lives in
/// <c>System.Data.Common</c> — the BCL, not EF Core or Npgsql. Application therefore
/// stays free of infrastructure packages while still recognising a database outage.
/// Npgsql's exception derives from it.
/// </para>
///
/// <para>
/// Nothing here extracts <c>exception.Message</c> into the error. The detail belongs in
/// the logs; what reaches the client is a stable sentence plus a stable code.
/// </para>
/// </summary>
public static class ResultErrors
{
    /// <summary>
    /// Classifies a failure from one of our own dependencies: the database is
    /// <see cref="ErrorKind.Infrastructure"/>, an HTTP call is
    /// <see cref="ErrorKind.Upstream"/>, and anything else is a bug.
    /// </summary>
    public static Error From(Exception exception, string operation) => exception switch
    {
        DbException => Error.Infrastructure(
            $"Failed to {operation}. Please try again later.", "database_unavailable"),

        HttpRequestException => Error.Upstream(
            $"Failed to {operation}. Please try again later."),

        // Deliberately not treated as an infrastructure failure: a NullReferenceException
        // is a defect, and reporting it as 503 would teach clients to retry a bug.
        _ => Error.Unexpected($"Failed to {operation}.")
    };

    /// <summary>
    /// Classifies a failure from an external service. Both a failed request and an
    /// unreadable response are <see cref="ErrorKind.Upstream"/>: in each case the
    /// problem is on the other side of the wire, not in this process.
    /// </summary>
    public static Error FromExternalService(Exception exception, string operation) => exception switch
    {
        HttpRequestException => Error.Upstream(
            $"Failed to {operation}. Please try again later.", "notion_unavailable"),

        // Either the payload could not be parsed, or the parser hit a shape it did not
        // expect. Both mean the response was unreadable, which is an upstream problem.
        InvalidOperationException or System.Text.Json.JsonException => Error.Upstream(
            $"Failed to {operation}: the response could not be read.", "notion_invalid_response"),

        _ => Error.Unexpected($"Failed to {operation}.")
    };
}
