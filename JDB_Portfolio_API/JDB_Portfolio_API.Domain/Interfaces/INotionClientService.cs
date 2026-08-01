using JDB_Portfolio_API.Application.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface INotionClientService
{
    Task<List<NotionPageCardDto>> QueryPageAsync(string pageId);
}
