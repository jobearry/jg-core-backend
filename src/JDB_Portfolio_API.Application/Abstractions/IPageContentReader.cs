using JDB_Portfolio_API.Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Abstractions;

/// <summary>
/// Reads published page content from an external source.
/// </summary>
/// <remarks>
/// The port is here rather than in <c>Domain</c> (plan 3.2 / KI-045). A vendor-neutral
/// interface in the core is still an integration concern wearing a neutral name, and
/// nothing in the model would ever call it.
///
/// <para>
/// The implementation cannot live next to it: <c>ExternalService</c> may only reference
/// <c>Domain</c> (AGENTS §6 rule 2), so the adapter that bridges this port to the Notion
/// client sits in the composition root, the only project that can see both. See
/// ADR-0005.
/// </para>
/// </remarks>
public interface IPageContentReader
{
    Task<IReadOnlyList<PageContent>> ReadPageAsync(string pageId, CancellationToken cancellationToken);
}
