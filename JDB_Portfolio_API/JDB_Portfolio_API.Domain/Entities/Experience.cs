using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public partial class Experience
{
    public int Id { get; set; }

    public string? CompanyName { get; set; }

    public DateTime? StartedAt { get; set; }

    public string? Description { get; set; }

    public string Role { get; set; } = null!;

    public string? Responsibility { get; set; }

    public string? Type { get; set; }
}
