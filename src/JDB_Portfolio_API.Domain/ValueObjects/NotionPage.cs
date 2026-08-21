using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.ValueObjects;

public class NotionPage
{
    public string Url { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
