using Portfolio.Application.Common;

namespace Portfolio.Endpoints;

/// <summary>
/// The single place where an application <see cref="Result{T}"/> becomes an HTTP
/// response. Before this, every endpoint hand-rolled
/// <c>Results.Problem(result.Error, statusCode: 502)</c>, which meant every failure —
/// a missing row, a bad argument, a database outage, a bug — came back as the same
/// status code with the same shape.
///
/// <para>
/// This is the only mapping from <see cref="ErrorKind"/> to a status code in the
/// solution. Application stays HTTP-agnostic; Presentation decides what a kind means.
/// </para>
/// </summary>
public static class ApiResult
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Error!.ToHttpResult();
    }

    /// <summary>
    /// A created resource keeps the same error mapping but returns 201 and a Location
    /// header on success. The location is built from the DTO, never from a domain type.
    /// </summary>
    public static IResult ToCreatedHttpResult<T>(this Result<T> result, Func<T, string> location)
    {
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttpResult();
        }

        return Results.Created(location(result.Value!), result.Value);
    }

    /// <summary>
    /// A delete has no body; failures still use the single error mapping.
    /// </summary>
    public static IResult ToNoContentHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.NoContent() : result.Error!.ToHttpResult();

    /// <summary>
    /// The same mapping for an error that never had a value to return — an argument
    /// guard in an endpoint, for instance.
    /// </summary>
    public static IResult ToHttpResult(this Error error) =>
        Results.Problem(
            title: TitleFor(error.Kind),
            detail: error.Message,
            statusCode: StatusCodeFor(error.Kind),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    /// <summary>
    /// Kind → status code.
    ///
    /// <list type="bullet">
    ///   <item><see cref="ErrorKind.Infrastructure"/> is <b>503</b>, not 500: our
    ///   database being down is an outage a client can retry through, not a defect.</item>
    ///   <item><see cref="ErrorKind.Upstream"/> is <b>502</b>: a service we call
    ///   returned a bad response, which is exactly what "bad gateway" means.</item>
    ///   <item><see cref="ErrorKind.Unexpected"/> is <b>500</b>: nothing predicted it.</item>
    /// </list>
    /// </summary>
    public static int StatusCodeFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Upstream => StatusCodes.Status502BadGateway,
        ErrorKind.Infrastructure => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => "Not found",
        ErrorKind.Validation => "Invalid request",
        ErrorKind.Unauthorized => "Unauthorized",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Upstream => "Upstream service unavailable",
        ErrorKind.Infrastructure => "Service temporarily unavailable",
        _ => "Unexpected error"
    };
}
