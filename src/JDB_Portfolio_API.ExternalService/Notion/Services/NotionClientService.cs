using JDB_Portfolio_API.ExternalService.Notion.Utils;
using JDB_Portfolio_API.ExternalService.Notion.DTO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using JDB_Portfolio_API.ExternalService.Notion.Models;

namespace JDB_Portfolio_API.ExternalService.Notion.Services;

/// <summary>
/// The Notion call itself. Deliberately implements no interface: the application's port
/// (<c>IPageContentReader</c>) is bridged to this class in the composition root, so this
/// project keeps referencing <c>Domain</c> only (plan 3.2 / KI-045, ADR-0005).
/// </summary>
/// <remarks>
/// The <see cref="HttpClient"/> is configured by the composition root (base address, auth
/// headers, 15s timeout), so this class knows only the path it calls. A transient failure
/// (429 or 5xx) is retried once; a network failure and a timeout are not, because a retry
/// would double the wait the caller already paid.
/// </remarks>
public class NotionClientService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;

    public NotionClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches a Notion page as markdown.
    /// </summary>
    /// <remarks>
    /// There is deliberately no <c>try/catch</c> here. The old code caught two exception
    /// types and rethrew a new instance with the same message, which threw away the
    /// original stack trace and any inner exception while adding nothing: the caller
    /// classifies by exception type, and plan 2.2 stopped publishing exception messages.
    /// Letting the original exception escape keeps the diagnostic detail that the
    /// Application layer now logs.
    /// </remarks>
    public async Task<List<NotionPage>> QueryPageAsync(string pageId, CancellationToken cancellationToken)
    {
        // Relative to the BaseAddress configured in AddExternalServices.
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
                // A 200 with no markdown is an upstream contract violation. Classified as a
                // 502 by the Application layer, not as a bug in this process.
                throw new InvalidOperationException($"Notion returned no markdown for page '{pageId}'.");
            }

            return NotionPageParserUtility.Parse(result.Markdown);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
}

