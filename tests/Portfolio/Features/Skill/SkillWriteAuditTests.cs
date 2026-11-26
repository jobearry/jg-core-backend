using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Portfolio.Domain.Entities;
using Portfolio.Features.Skill;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Disconnected updates must not clear the database-owned created_at column.
/// The interceptor stops SaveChanges before a connection is opened, so the entry
/// state can be inspected without a real database.
/// </summary>
public sealed class SkillWriteAuditTests
{
    private const string UnusedConnection =
        "Host=unused;Database=unused;Username=unused;Password=unused";

    [Fact]
    public async Task UpdateAsync_ForTechStack_DoesNotMarkCreatedAtModified()
    {
        var interceptor = new CapturingSaveChangesInterceptor();
        using var readContext = new PortfolioContext(ReadOptions());
        using var writeContext = new PortfolioWriteContext(WriteOptions(interceptor));
        var repository = new EfSkillRepository(readContext, writeContext);
        var stack = new TechStack(5, "C#", category: "Backend");

        await Assert.ThrowsAsync<StopBeforeDatabase>(
            () => repository.UpdateAsync(stack, 2, CancellationToken.None));

        var entry = interceptor.StackEntry;
        Assert.NotNull(entry);
        Assert.False(entry.Property(e => e.CreatedAt).IsModified);
    }

    [Fact]
    public async Task UpdateAsync_ForCategory_DoesNotMarkCreatedAtModified()
    {
        var interceptor = new CapturingSaveChangesInterceptor();
        using var readContext = new PortfolioContext(ReadOptions());
        using var writeContext = new PortfolioWriteContext(WriteOptions(interceptor));
        var repository = new EfTechStackCategoryRepository(readContext, writeContext);
        var category = new TechStackCategory(2, "Backend");

        await Assert.ThrowsAsync<StopBeforeDatabase>(
            () => repository.UpdateAsync(category, CancellationToken.None));

        var entry = interceptor.CategoryEntry;
        Assert.NotNull(entry);
        Assert.False(entry.Property(e => e.CreatedAt).IsModified);
    }

    private static DbContextOptions<PortfolioContext> ReadOptions() =>
        new DbContextOptionsBuilder<PortfolioContext>()
            .UseNpgsql(UnusedConnection)
            .Options;

    private static DbContextOptions<PortfolioWriteContext> WriteOptions(
        SaveChangesInterceptor interceptor) =>
        new DbContextOptionsBuilder<PortfolioWriteContext>()
            .UseNpgsql(UnusedConnection)
            .AddInterceptors(interceptor)
            .Options;

    private sealed class CapturingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public EntityEntry<TechStackEntity>? StackEntry { get; private set; }

        public EntityEntry<TechStackCategoryEntity>? CategoryEntry { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            StackEntry = eventData.Context?.ChangeTracker
                .Entries<TechStackEntity>()
                .SingleOrDefault();
            CategoryEntry = eventData.Context?.ChangeTracker
                .Entries<TechStackCategoryEntity>()
                .SingleOrDefault();
            throw new StopBeforeDatabase();
        }
    }

    private sealed class StopBeforeDatabase : Exception { }
}
