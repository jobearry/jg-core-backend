using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public partial class TechStackCategory
{
    public int Id { get; set; }

    public string Category { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
}
