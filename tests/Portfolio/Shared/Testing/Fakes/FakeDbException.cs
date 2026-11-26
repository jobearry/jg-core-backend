using System.Data.Common;

namespace Portfolio.Api.Tests.Fakes;

/// <summary>
/// Simulates a database failure. <see cref="DbException"/> has protected constructors,
/// so subclassing is the only way to create one without taking a dependency on Npgsql
/// in the test projects.
/// </summary>
public sealed class FakeDbException(string message) : DbException(message);
