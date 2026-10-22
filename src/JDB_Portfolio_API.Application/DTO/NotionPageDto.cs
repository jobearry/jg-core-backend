namespace JDB_Portfolio_API.Application.DTO;

/// <summary>
/// The published shape of a Notion page. This exists so that
/// <c>Domain.ValueObjects.NotionPage</c> can eventually leave the domain — it is an
/// integration concept, not a domain one (plan 3.2).
/// </summary>
public sealed record NotionPageDto(
    string Url,
    string Date,
    string Title);
