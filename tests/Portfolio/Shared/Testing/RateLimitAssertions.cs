using System.Net;

namespace Portfolio.Api.Tests;

/// <summary>
/// Sends the same request until the limiter rejects it. Keeping the loop in one
/// place stops the permit count from drifting between endpoint tests.
/// </summary>
public static class RateLimitAssertions
{
    public static async Task<HttpResponseMessage> SendUntilRejectedAsync(
        Func<Task<HttpResponseMessage>> send,
        int permitLimit)
    {
        for (var i = 0; i < permitLimit; i++)
        {
            var allowed = await send();
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await send();
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        return rejected;
    }
}
