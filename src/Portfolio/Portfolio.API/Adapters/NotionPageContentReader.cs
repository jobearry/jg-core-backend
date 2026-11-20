using Portfolio.Application.Abstractions;
using Portfolio.Application.Models;
using Portfolio.ExternalService.Notion.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Adapters;

/// <summary>
/// Bridges <see cref="IPageContentReader"/> to the Notion client.
/// </summary>
/// <remarks>
/// This class exists because of a project-reference rule, not a design preference.
/// <c>Application</c> owns the port and cannot reference <c>ExternalService</c>;
/// <c>ExternalService</c> implements the vendor call and cannot reference
/// <c>Application</c> (AGENTS §6 rule 2). The composition root is the only project that
/// sees both, so the translation lives here — thin by design, with no logic beyond the
/// field-by-field mapping. See ADR-0005.
/// </remarks>
public sealed class NotionPageContentReader : IPageContentReader
{
    private readonly NotionClientService _client;

    public NotionPageContentReader(NotionClientService client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<PageContent>> ReadPageAsync(
        string pageId,
        CancellationToken cancellationToken)
    {
        var pages = await _client.QueryPageAsync(pageId, cancellationToken);

        return pages
            .Select(page => new PageContent(page.Url, page.Date, page.Title))
            .ToList();
    }
}
