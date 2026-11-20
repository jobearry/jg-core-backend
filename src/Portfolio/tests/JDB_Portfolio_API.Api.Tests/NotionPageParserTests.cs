using JDB_Portfolio_API.ExternalService.Notion.Utils;

namespace JDB_Portfolio_API.Api.Tests;

/// <summary>
/// Plan 4.7. The parser used to wrap the markdown in &lt;root&gt; and parse it as XML, so
/// malformed content failed the whole request. These tests pin the tolerant behaviour.
/// </summary>
public class NotionPageParserTests
{
    [Fact]
    public void Parse_ReturnsEveryCompletePageCard()
    {
        const string markdown =
            "Intro\n" +
            "<page url=\"https://notion.so/a\"><mention-date start=\"2026-01-02\"/>First</page>\n" +
            "<page url=\"https://notion.so/b\"><mention-date start=\"2026-02-03\"/>Second</page>";

        var pages = NotionPageParserUtility.Parse(markdown);

        Assert.Collection(
            pages,
            page =>
            {
                Assert.Equal("https://notion.so/a", page.Url);
                Assert.Equal("2026-01-02", page.Date);
                Assert.Equal("First", page.Title);
            },
            page =>
            {
                Assert.Equal("https://notion.so/b", page.Url);
                Assert.Equal("2026-02-03", page.Date);
                Assert.Equal("Second", page.Title);
            });
    }

    [Fact]
    public void Parse_SkipsAnUnclosedPageAndKeepsTheNextCard()
    {
        const string markdown =
            "<page url=\"https://notion.so/broken\">Broken\n" +
            "<page url=\"https://notion.so/good\"><mention-date start=\"2026-03-04\"/>Good</page>";

        var page = Assert.Single(NotionPageParserUtility.Parse(markdown));

        Assert.Equal("https://notion.so/good", page.Url);
        Assert.Equal("2026-03-04", page.Date);
        Assert.Equal("Good", page.Title);
    }

    [Fact]
    public void Parse_DoesNotThrowOnALiteralRootClosingTag()
    {
        const string markdown =
            "<page url=\"https://notion.so/c\"><mention-date start=\"2026-05-06\"/>Tom &amp; Jerry </root> and friends</page>";

        var page = Assert.Single(NotionPageParserUtility.Parse(markdown));

        Assert.Equal("Tom & Jerry and friends", page.Title);
    }

    [Fact]
    public void Parse_ReturnsNoCardsForAnEmptyResponse()
    {
        Assert.Empty(NotionPageParserUtility.Parse(string.Empty));
    }
}
