using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Settings;
using JDB_Portfolio_API.Persistence.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Services;

public class NotionClientService : INotionClientService
{
    private readonly HttpClient _httpClient;
    private readonly NotionSettings _opts;

    public NotionClientService(HttpClient httpClient, IOptions<NotionSettings> opts)
    {
        _httpClient = httpClient;
        _opts = opts.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _opts.Token);
        _httpClient.DefaultRequestHeaders.Add("Notion-Version", _opts.Version);
    }

    public async Task<List<NotionPageCardDto>> QueryPageAsync(string pageId)
    {
        var response = await _httpClient.GetAsync(
            $"https://api.notion.com/v1/pages/{pageId}/markdown"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NotionPageMarkdownDto>();

        if (result is null)
            throw new InvalidOperationException("Failed to deserialize Notion page markdown.");

        var parsedResult = NotionPageParserUtility.Parse(result.Markdown);
        return parsedResult;
    }

}
