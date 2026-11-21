using System.Net;
using System.Net.Http.Json;

namespace Portfolio.Features.Notion;

/// <summary>
/// The Notion call itself. The <see cref="HttpClient"/> is configured by the composition
/// root (base address, auth headers, 15s timeout), so this class knows only the path it
/// calls. A transient failure (429 or 5xx) is retried once; a network failure and a
/// timeout are not, because a retry would double the wait the caller already paid.
/// </summary>
public class NotionClientService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;

    public NotionClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches a Notion page as markdown. Upstream exceptions escape unchanged so the
    /// handler can distinguish an outage from an unreadable response.
    /// </summary>
    public async Task<List<NotionPage>> QueryPageAsync(string pageId, CancellationToken cancellationToken)
    {
        var path = $"v1/pages/{pageId}/markdown";

        var response = await _httpClient.GetAsync(path, cancellationToken);

        if (IsTransient(response.StatusCode))
        {
            response.Dispose();
            await Task.Delay(RetryDelay, cancellationToken);
            response = await _httpClient.GetAsync(path, cancellationToken);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Notion returned {(int)response.StatusCode} for page '{pageId}'.",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<NotionPageMarkdownDto>(cancellationToken);

            if (result?.Markdown is null)
            {
                throw new InvalidOperationException($"Notion returned no markdown for page '{pageId}'.");
            }

            return NotionPageParserUtility.Parse(result.Markdown);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
}
