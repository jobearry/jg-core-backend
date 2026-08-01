using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public partial class Project
{
    public int Id { get; set; }

    public int? ExperienceId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Contribution { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
