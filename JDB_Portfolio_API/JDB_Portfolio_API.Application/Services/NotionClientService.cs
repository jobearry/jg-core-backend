using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;

namespace JDB_Portfolio_API.Application.Services;

public interface INotionClientService
{
    Task<List<NotionPageCardDto>> QueryPageAsync(string pageId);
}
public class NotionClientService : INotionClientService
{
    private readonly HttpClient _httpClient;
    private readonly NotionSettings _notionSettings;

    public NotionClientService(HttpClient httpClient, IOptions<NotionSettings> notionSettings)
    {
        _httpClient = httpClient;
        _notionSettings = notionSettings.Value;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _notionSettings.Token);
        _httpClient.DefaultRequestHeaders.Add("Notion-Version", _notionSettings.Version);
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

        var parsedResult = Parse(result.Markdown);
        return parsedResult;
    }

    private static List<NotionPageCardDto> Parse(string markdown)
    {
        // Wrap root because input has multiple top-level nodes
        var xml = $"<root>{markdown}</root>";

        var doc = XDocument.Parse(xml);

        var cards = doc.Descendants("page")
            .Select(page =>
            {
                var url = (string)page.Attribute("url")! ?? "";

                var mentionDate = page.Descendants("mention-date").FirstOrDefault();

                var date = (string)mentionDate?.Attribute("start")! ?? "";

                var title = page.Value
                    .Replace("—", "")
                    .Trim();

                return new NotionPageCardDto
                {
                    Url = url,
                    Date = date,
                    Title = title
                };
            })
            .ToList();

        return cards;
    }
}
