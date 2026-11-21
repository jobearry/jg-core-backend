namespace Portfolio.Persistence.Tests;

/// <summary>
/// Shared setup for the mapping tests. The dispatcher is stateless, so one
/// instance is enough for the whole class.
/// </summary>
public abstract class MappingTestBase
{
    protected static readonly HandWrittenMapper Mapper = new();
}
