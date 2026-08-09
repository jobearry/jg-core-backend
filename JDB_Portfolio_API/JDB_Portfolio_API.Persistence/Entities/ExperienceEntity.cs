using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Persistence.Entities;

public partial class ExperienceEntity
{
    public int Id { get; set; }

    public string? CompanyName { get; set; }

    public DateTime? StartedAt { get; set; }

    public string? Description { get; set; }

    public string Role { get; set; } = null!;

    public string? Responsibility { get; set; }

    public string? Type { get; set; }

    public ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
}
