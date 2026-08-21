using System;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Domain.Entities;

public partial class Recommendation
{
    public int Id { get; set; }

    public string PersonName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string CareerPosition { get; set; } = null!;

    public DateTime PostedAt { get; set; }
}
