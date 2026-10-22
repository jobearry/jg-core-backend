using JDB_Portfolio_API.ExternalService.Notion.Settings;
using JDB_Portfolio_API.ExternalService.Notion.Utils;
using JDB_Portfolio_API.ExternalService.Notion.DTO;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
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
public class NotionClientService
{
    private readonly HttpClient _httpClient;
    private readonly NotionSettings _opts;

    public NotionClientService(HttpClient httpClient, IOptions<NotionSettings> opts)
    {
        _httpClient = httpClient;
        _opts = opts.Value;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.Token);
        _httpClient.DefaultRequestHeaders.Add("Notion-Version", _opts.Version);
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
        var response = await _httpClient.GetAsync(
            $"https://api.notion.com/v1/pages/{pageId}/markdown",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Notion returned {(int)response.StatusCode} for page '{pageId}'.");
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
