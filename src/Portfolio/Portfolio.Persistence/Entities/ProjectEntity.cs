using System;
using System.Collections.Generic;

namespace Portfolio.Persistence.Entities;

public partial class ProjectEntity
{
    public int Id { get; set; }

    public int? ExperienceId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Contribution { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ExperienceEntity? Experience { get; set; }
    public ICollection<ProjectTechStack> ProjectTechStacks { get; set; } = new List<ProjectTechStack>();
}
