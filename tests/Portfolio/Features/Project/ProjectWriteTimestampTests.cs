using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Portfolio.Features.Project;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// The EF adapter owns the persistence-only edit timestamp. The interceptor stops
/// SaveChanges before a connection is opened, so this proves what would be written
/// without needing a database.
/// </summary>
public sealed class ProjectWriteTimestampTests
{
    private const string UnusedConnection =
        "Host=unused;Database=unused;Username=unused;Password=unused";

    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtBeforeSaving()
    {
        var interceptor = new CapturingSaveChangesInterceptor();
        using var readContext = new PortfolioContext(ReadOptions());
        using var writeContext = new PortfolioWriteContext(WriteOptions(interceptor));
        var repository = new EfProjectRepository(
            readContext, writeContext, new FixedTimeProvider(FixedNow));
        var project = new DomainProject(7, "Name");

        await Assert.ThrowsAsync<StopBeforeDatabase>(
            () => repository.UpdateAsync(project, CancellationToken.None));

        Assert.Equal(FixedNow.UtcDateTime, interceptor.Project?.UpdatedAt);
        Assert.Equal(DateTimeKind.Unspecified, interceptor.Project?.UpdatedAt?.Kind);
        Assert.False(interceptor.ProjectEntry?.Property(e => e.CreatedAt).IsModified);
    }

    private static DbContextOptions<PortfolioContext> ReadOptions() =>
        new DbContextOptionsBuilder<PortfolioContext>()
            .UseNpgsql(UnusedConnection)
            .Options;

    private static DbContextOptions<PortfolioWriteContext> WriteOptions(SaveChangesInterceptor interceptor) =>
        new DbContextOptionsBuilder<PortfolioWriteContext>()
            .UseNpgsql(UnusedConnection)
            .AddInterceptors(interceptor)
            .Options;

    private sealed class CapturingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public ProjectEntity? Project { get; private set; }

        public EntityEntry<ProjectEntity>? ProjectEntry { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ProjectEntry = eventData.Context?.ChangeTracker
                .Entries<ProjectEntity>()
                .Single();
            Project = ProjectEntry?.Entity;
            throw new StopBeforeDatabase();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StopBeforeDatabase : Exception { }
}
