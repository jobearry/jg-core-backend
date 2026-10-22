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

    /// <summary>
    /// The position the person holds. <c>null</c> means it is deliberately not
    /// shown — some recommenders do not want their title published. The mapping
    /// turns a blank database value into this.
    /// </summary>
    public Role? CareerPosition { get; set; }

    public DateTime PostedAt { get; set; }

    public Recommendation(int id, string personName, string description, Role? careerPosition, DateTime postedAt)
    {
        Id = id;
        PersonName = personName ?? throw new ArgumentNullException(nameof(personName)); ;
        Description = description;
        CareerPosition = careerPosition;
        PostedAt = postedAt;
    }
}
