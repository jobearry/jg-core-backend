namespace JDB_Portfolio_API.Application.Common;

/// <summary>
/// What kind of thing went wrong. The API layer is the only place that turns this into
/// an HTTP status code, so Application stays HTTP-agnostic.
/// </summary>
public enum ErrorKind
{
    /// <summary>The requested record does not exist.</summary>
    NotFound,

    /// <summary>The caller sent something invalid.</summary>
    Validation,

    /// <summary>The caller is not allowed to do this — a missing or wrong credential.</summary>
    Unauthorized,

    /// <summary>The request conflicts with current state.</summary>
    Conflict,

    /// <summary>Our own infrastructure failed — the database is unreachable.</summary>
    Infrastructure,

    /// <summary>A service we call failed — today, Notion.</summary>
    Upstream,

    /// <summary>A bug. Nothing predicted this.</summary>
    Unexpected
}

/// <summary>
/// A typed failure, replacing the bare string that every service used to return.
///
/// <para>
/// <see cref="Code"/> is the stable, machine-readable half, so a client can branch on
/// a value instead of matching prose. <see cref="Message"/> is the human half and
/// deliberately carries <b>no exception detail</b>: Npgsql messages can contain host
/// names, database names and SQL, and this value is published to callers.
/// </para>
/// </summary>
public sealed record Error(ErrorKind Kind, string Code, string Message)
{
    public static Error NotFound(string message, string code = "not_found") =>
        new(ErrorKind.NotFound, code, message);

    public static Error Validation(string message, string code = "validation_failed") =>
        new(ErrorKind.Validation, code, message);

    public static Error Unauthorized(string message, string code = "unauthorized") =>
        new(ErrorKind.Unauthorized, code, message);

    public static Error Conflict(string message, string code = "conflict") =>
        new(ErrorKind.Conflict, code, message);

    public static Error Infrastructure(string message, string code = "database_unavailable") =>
        new(ErrorKind.Infrastructure, code, message);

    public static Error Upstream(string message, string code = "upstream_unavailable") =>
        new(ErrorKind.Upstream, code, message);

    public static Error Unexpected(string message, string code = "unexpected_error") =>
        new(ErrorKind.Unexpected, code, message);
}
