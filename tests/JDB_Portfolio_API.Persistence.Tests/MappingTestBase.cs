using AutoMapper;
using JDB_Portfolio_API.Persistence.Profiles;
using Microsoft.Extensions.Logging.Abstractions;

namespace JDB_Portfolio_API.Persistence.Tests;

/// <summary>
/// Shared setup for the mapping tests. One mapper for the whole class, because
/// building a <see cref="MapperConfiguration"/> is not free and the profile is
/// stateless.
/// </summary>
public abstract class MappingTestBase
{
    protected static readonly IMapper Mapper = CreateMapper();

    /// <summary>
    /// AutoMapper 16 requires a logger factory. The mapper logs nothing in these
    /// tests, so a null factory is enough.
    /// </summary>
    protected static MapperConfiguration CreateConfiguration() =>
        new(c => c.AddProfile<PortfolioMappingProfile>(), NullLoggerFactory.Instance);

    private static IMapper CreateMapper() => CreateConfiguration().CreateMapper();
}
