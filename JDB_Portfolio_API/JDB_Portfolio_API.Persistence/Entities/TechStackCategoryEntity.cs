using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Persistence.Entities;

public partial class TechStackCategoryEntity
{
    public int Id { get; set; }

    public string Category { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
    public ICollection<TechStackEntity> TechStacks { get; set; } = new List<TechStackEntity>();
}
