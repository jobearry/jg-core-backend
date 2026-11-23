namespace Portfolio.Api.Tests;

/// <summary>
/// The Notion markdown payload shared by the client and reader tests. Both need the
/// same vendor shape; keeping one copy avoids a silent drift between the two chains.
/// </summary>
public static class NotionTestPayloads
{
    public const string MarkdownResponse =
        """{"object":"page","id":"page-id","markdown":"<page url=\"https://notion.so/page\"><mention-date start=\"2026-06-27\"/>About me</page>","truncated":false,"unknownBlockIds":[],"requestId":"req"}""";
}
