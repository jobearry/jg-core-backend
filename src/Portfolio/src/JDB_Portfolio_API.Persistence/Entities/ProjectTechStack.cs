using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Entities;

public class ProjectTechStack
{
    public int ProjectId { get; set; }
    public ProjectEntity Project { get; set; } = null!;
    public int TechStackId { get; set; }
    public TechStackEntity TechStack { get; set; } = null!;
}
