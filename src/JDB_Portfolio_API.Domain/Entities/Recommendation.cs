using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Data;

namespace JDB_Portfolio_API.Domain.Entities;

public class Recommendation
{
    public int Id { get; set; }

    public string PersonName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public Role CareerPosition { get; set; } = null!;

    public DateTime PostedAt { get; set; }

    public Recommendation(int id, string personName, string description, Role careerPosition, DateTime postedAt)
    {
        Id = id;
        PersonName = personName ?? throw new ArgumentNullException(nameof(personName)); ;
        Description = description;
        CareerPosition = careerPosition ?? throw new ArgumentNullException(nameof(careerPosition));
        PostedAt = postedAt;
    }
}
