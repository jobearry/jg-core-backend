using Portfolio.Application.Common;
using Microsoft.Extensions.Logging;

namespace Portfolio.Features.Notion;

/// <summary>
/// The Notion use case: read the pages and turn upstream failures into typed results.
/// </summary>
public sealed class GetNotionPageHandler
{
    private readonly IPageContentReader _reader;
    private readonly ILogger<GetNotionPageHandler> _logger;

    public GetNotionPageHandler(
        IPageContentReader reader,
        ILogger<GetNotionPageHandler> logger)
    {
        _reader = reader;
        _logger = logger;
    }

    public async Task<Result<List<NotionPageDto>>> HandleAsync(
        string pageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await _reader.ReadPageAsync(pageId, cancellationToken);
            return Result<List<NotionPageDto>>.Success(
                page.Select(notionPage => notionPage.ToDto()).ToList());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A failed request and an unreadable response are both the other side's
            // problem, so both become 502 — but with different codes, so a client can
            // tell "Notion is down" from "Notion changed its format".
            var error = ResultErrors.FromExternalService(ex, "query the Notion page");
            _logger.LogWarning(
                ex,
                "Dependency failure while trying to {Operation} ({ErrorCode}).",
                "query the Notion page",
                error.Code);

            return Result<List<NotionPageDto>>.Failure(error);
        }
    }
}
