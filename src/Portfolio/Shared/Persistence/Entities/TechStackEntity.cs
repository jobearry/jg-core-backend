using System;
using System.Collections.Generic;

namespace Portfolio.Persistence.Entities;

public partial class TechStackEntity
{
    public int Id { get; set; }

    public string StackName { get; set; } = null!;

    public string? ImgSrc { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int TechStackCategoryId { get; set; }

    public ICollection<ProjectTechStack> ProjectTechStacks { get; set; } = new List<ProjectTechStack>();
    public TechStackCategoryEntity TechStackCategory { get; set; } = null!;
}
