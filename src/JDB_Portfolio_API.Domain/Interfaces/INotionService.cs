using JDB_Portfolio_API.Domain.ValueObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface INotionService
{
    Task<List<NotionPage>> QueryPageAsync(string pageId, CancellationToken cancellationToken);
}
