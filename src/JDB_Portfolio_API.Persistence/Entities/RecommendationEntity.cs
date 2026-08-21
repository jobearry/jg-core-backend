using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Persistence.Entities;

public partial class RecommendationEntity
{
    public int Id { get; set; }

    public string PersonName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string CareerPosition { get; set; } = null!;

    public DateTime PostedAt { get; set; }
}
