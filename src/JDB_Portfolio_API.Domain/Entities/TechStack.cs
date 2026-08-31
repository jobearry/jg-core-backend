using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public partial class TechStack
{
    public int Id { get; set; }

    public string StackName { get; set; } = null!;

    public string? ImgSrc { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Category { get; set; } = null!;
}
