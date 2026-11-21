namespace Portfolio.Features.Notion;

/// <summary>
/// Maps the slice's internal page model to its published wire shape.
/// </summary>
public static class NotionMapper
{
    public static NotionPageDto ToDto(this PageContent pageContent) =>
        new(pageContent.Url, pageContent.Date, pageContent.Title);
}
