using System.Data.Common;
using Npgsql;

namespace JDB_Portfolio_API.Persistence.Tests;

/// <summary>
/// Plan 2.2 classifies a database outage by catching <see cref="DbException"/> in the
/// Application layer. It cannot catch <c>NpgsqlException</c>, because Application must
/// not reference the provider — so the whole 503 mapping rests on one assumption: the
/// provider's failures derive from <see cref="DbException"/>.
///
/// <para>
/// This test pins that assumption, because the failure mode is silent: if Npgsql ever
/// stopped deriving from <see cref="DbException"/>, a database outage would quietly
/// become a 500 "unexpected error" instead of a 503, and no existing test would notice.
/// </para>
///
/// <para>
/// It connects to a port nothing listens on, so it fails immediately and touches no
/// database.
/// </para>
/// </summary>
public class NpgsqlFailureTypeTests
{
    [Fact]
    public async Task AConnectionFailure_IsADbException()
    {
        var exception = await Record.ExceptionAsync(async () =>
        {
            await using var connection = new NpgsqlConnection(
                "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1");
            await connection.OpenAsync();
        });

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<DbException>(exception);
    }
}
