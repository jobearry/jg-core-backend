using System;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// When a role was held: a start date, and an end date that is <c>null</c> while the
/// role is ongoing.
/// </summary>
/// <remarks>
/// A <c>record</c>: two periods with the same dates are the same period.
///
/// <para>
/// The constructor enforces what a period can be: a real start date, a real end date
/// when there is one, an end that is not before the start, and a start that is not in
/// the future — a period describes something that has begun. Both dates are nullable
/// because the source data is: an unknown start and an unknown end are different facts,
/// and <see cref="IsOngoing"/> is what tells the second one apart.
/// </para>
/// </remarks>
public sealed record Period
{
    public DateTime? StartedAt { get; }

    public DateTime? FinishedAt { get; }

    public Period(DateTime? startedAt = null, DateTime? finishedAt = null)
    {
        if (startedAt.HasValue && startedAt.Value == default)
            throw new ArgumentException("StartedAt must be a valid date.", nameof(startedAt));

        if (finishedAt.HasValue && finishedAt.Value == default)
            throw new ArgumentException("FinishedAt must be a valid date.", nameof(finishedAt));

        // Compared by date, not by instant: a start of "today" is legitimate in every
        // timezone this API serves, and the time of day must never decide it.
        if (startedAt.HasValue && startedAt.Value.Date > DateTime.UtcNow.Date)
            throw new ArgumentException("A role cannot start in the future.", nameof(startedAt));

        if (finishedAt < startedAt)
            throw new ArgumentException("FinishedAt cannot be before StartedAt.", nameof(finishedAt));

        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    /// <summary>
    /// The role is still held: it has started and has no end date. False when nothing
    /// is known at all — an unknown start with no end is not "ongoing", it is
    /// "unknown".
    /// </summary>
    public bool IsOngoing => StartedAt.HasValue && !FinishedAt.HasValue;

    /// <summary>
    /// Elapsed time so far, measured against now while the role is ongoing.
    /// <c>null</c> when there is no start date. Prefer
    /// <c>PortfolioDtoMapper.DurationInMonths</c> for anything published: raw
    /// <c>TimeSpan</c> does not survive the wire.
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (!StartedAt.HasValue) return null;

            var effectiveEnd = FinishedAt ?? DateTime.UtcNow;
            return effectiveEnd - StartedAt.Value;
        }
    }

    public override string ToString() => $"{StartedAt:O} - {FinishedAt:O}";
}
