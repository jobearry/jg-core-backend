using AutoMapper;
using Portfolio.Persistence.Profiles;

namespace Portfolio.Persistence.Tests;

public class MappingConfigurationTests : MappingTestBase
{
    /// <summary>
    /// The profile is the highest-risk file in the solution. This gate makes an
    /// unmapped destination member a test failure instead of a silently dropped
    /// value. It does not catch a bad type conversion — only a runtime map does.
    /// </summary>
    [Fact]
    public void MappingConfiguration_HasNoUnmappedMembers()
    {
        CreateConfiguration().AssertConfigurationIsValid();
    }
}
