using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.ExternalService.Notion.Settings;
using JDB_Portfolio_API.ExternalService.Notion.Utils;
using JDB_Portfolio_API.ExternalService.Notion.DTO;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using JDB_Portfolio_API.Domain.ValueObjects;

namespace JDB_Portfolio_API.ExternalService.Notion.Services;

public class NotionClientService : INotionService
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

    public async Task<List<NotionPage>> QueryPageAsync(string pageId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://api.notion.com/v1/pages/{pageId}/markdown"
            );

            if (!response.IsSuccessStatusCode) throw new HttpRequestException();
            var result = await response.Content.ReadFromJsonAsync<NotionPageMarkdownDto>();

            var parsedResult = NotionPageParserUtility.Parse(result.Markdown);
            return parsedResult;

        }catch (HttpRequestException hre){
            throw new HttpRequestException(hre.Message);
        }
        catch (InvalidOperationException ioe){
            throw new InvalidOperationException(ioe.Message);
        }

    }

}
