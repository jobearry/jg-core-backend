using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.ValueObjects;

public sealed class Period
{
    public DateTime? StartedAt { get; }
    public DateTime? FinishedAt { get; }

    public Period (DateTime? startedAt = null, DateTime? finishedAt = null)
    {
        if (startedAt == default)
            throw new ArgumentException("StartedAt must be a valid date.");
        if (finishedAt == default)
            throw new ArgumentException("FinishedAt must be a valid date.");
        if (finishedAt < startedAt)
            throw new ArgumentException("FinishedAt cannot be before StartedAt.");

        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }
    public TimeSpan? Duration
    {
        get
        {
            if (!StartedAt.HasValue) return TimeSpan.Zero;

            var effectiveEnd = FinishedAt ?? DateTime.UtcNow;
            return effectiveEnd - StartedAt.Value;
        }
    }
    public override string ToString() => $"{StartedAt:O} - {FinishedAt:O}";
}
