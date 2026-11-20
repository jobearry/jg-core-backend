using JDB_Portfolio_API.ExternalService.Notion.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace JDB_Portfolio_API.ExternalService.Notion.Utils;

/// <summary>
/// Extracts the <c>&lt;page&gt;</c> cards from Notion's markdown response.
/// </summary>
/// <remarks>
/// This used to wrap the markdown in <c>&lt;root&gt;…&lt;/root&gt;</c> and hand it to
/// <see cref="System.Xml.Linq.XDocument"/>, which threw on any fragment that was not
/// well-formed XML — an unclosed tag, an attribute containing <c>&lt;</c>, or a literal
/// <c>&lt;/root&gt;</c> in the page text. The scan is deliberately tolerant instead: every
/// complete <c>&lt;page&gt;…&lt;/page&gt;</c> block becomes a card, and a broken fragment is
/// skipped rather than failing the whole request (plan 4.7).
/// </remarks>
public static class NotionPageParserUtility
{
    private static readonly Regex PagePattern = new(
        // The tempered dot stops the body at another <page> opener, so an unclosed tag
        // is skipped instead of swallowing the next, well-formed card.
        @"<page\b(?<attributes>[^>]*)>(?<body>(?:(?!<page\b).)*?)</page\s*>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex UrlPattern = new(
        @"\burl\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MentionDatePattern = new(
        @"<mention-date\b[^>]*?\bstart\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TagPattern = new(
        @"<[^>]*>",
        RegexOptions.Compiled);

    private static readonly Regex WhitespacePattern = new(
        @"\s+",
        RegexOptions.Compiled);

    public static List<NotionPage> Parse(string markdown)
    {
        var cards = new List<NotionPage>();

        if (string.IsNullOrEmpty(markdown))
        {
            return cards;
        }

        foreach (Match match in PagePattern.Matches(markdown))
        {
            var attributes = match.Groups["attributes"].Value;
            var body = match.Groups["body"].Value;

            cards.Add(new NotionPage
            {
                Url = ReadAttribute(UrlPattern, attributes),
                Date = ReadAttribute(MentionDatePattern, body),
                Title = ReadTitle(body)
            });
        }

        return cards;
    }

    private static string ReadAttribute(Regex pattern, string input)
    {
        var match = pattern.Match(input);

        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["value"].Value)
            : string.Empty;
    }

    private static string ReadTitle(string body)
    {
        var withoutTags = TagPattern.Replace(body, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutTags);

        return WhitespacePattern.Replace(decoded.Replace("—", string.Empty), " ").Trim();
    }
}
