using identity_service.Data;
using System.Reflection;
using Npgsql;

namespace identity_service.Tests;

/// <summary>
/// Non-Docker tests for UnitOfWork property accessors.
/// These only need a NpgsqlDataSource object (no real DB connection).
/// </summary>
public class DataTests : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public DataTests()
    {
        _dataSource = NpgsqlDataSource.Create("Host=localhost;Database=fake");
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }

    [Fact]
    public void UoW_Connection_BeforeBegin_ThrowsInvalidOperation()
    {
        var uow = new UnitOfWork(_dataSource);

        Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);
    }

    [Fact]
    public void UoW_Transaction_BeforeBegin_ReturnsNull()
    {
        var uow = new UnitOfWork(_dataSource);

        Assert.Null(uow.Transaction);
    }

    [Fact]
    public void UoW_Connection_WhenSet_ReturnsConnection()
    {
        var uow = new UnitOfWork(_dataSource);
        using var conn = new NpgsqlConnection("Host=localhost;Database=fake");

        // Set _connection via reflection to cover the non-null branch
        var field = typeof(UnitOfWork).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(uow, conn);

        Assert.NotNull(uow.Connection);
        Assert.Same(conn, uow.Connection);
    }
}
