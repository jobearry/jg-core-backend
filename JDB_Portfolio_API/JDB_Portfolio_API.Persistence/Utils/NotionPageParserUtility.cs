using JDB_Portfolio_API.Application.DTO;
using System;
using System.Xml.Linq;

namespace JDB_Portfolio_API.Persistence.Utils;

public static class NotionPageParserUtility
{
    public static List<NotionPageCardDto> Parse(string markdown)
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
