using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface INotionService
{
    Task<List<NotionPage>> QueryPageAsync(string pageId);
}
