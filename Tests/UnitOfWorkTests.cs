using identity_service.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace identity_service.Tests;

public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("""
            CREATE TABLE test_table (
                id   SERIAL PRIMARY KEY,
                name VARCHAR NOT NULL
            )
            """, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task BeginAsync_OpensConnectionAndTransaction()
    {
        await using var uow = new UnitOfWork(_dataSource);

        await uow.BeginAsync();

        Assert.NotNull(uow.Connection);
        Assert.NotNull(uow.Transaction);
    }

    [Fact]
    public void Connection_BeforeBegin_ThrowsInvalidOperation()
    {
        var uow = new UnitOfWork(_dataSource);

        Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);
    }

    [Fact]
    public async Task Transaction_BeforeBegin_ReturnsNull()
    {
        var uow = new UnitOfWork(_dataSource);

        Assert.Null(uow.Transaction);

        await uow.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_PersistsData()
    {
        await using var uow = new UnitOfWork(_dataSource);
        await uow.BeginAsync();

        await using var cmd = new NpgsqlCommand("INSERT INTO test_table (name) VALUES ('committed')", uow.Connection, uow.Transaction);
        await cmd.ExecuteNonQueryAsync();

        await uow.CommitAsync();

        // Verify data is persisted
        await using var verifyConn = await _dataSource.OpenConnectionAsync();
        await using var verifyCmd = new NpgsqlCommand("SELECT COUNT(*) FROM test_table WHERE name = 'committed'", verifyConn);
        var count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RollbackAsync_RevertsData()
    {
        await using var uow = new UnitOfWork(_dataSource);
        await uow.BeginAsync();

        await using var cmd = new NpgsqlCommand("INSERT INTO test_table (name) VALUES ('rolled_back')", uow.Connection, uow.Transaction);
        await cmd.ExecuteNonQueryAsync();

        await uow.RollbackAsync();

        // Verify data was not persisted
        await using var verifyConn = await _dataSource.OpenConnectionAsync();
        await using var verifyCmd = new NpgsqlCommand("SELECT COUNT(*) FROM test_table WHERE name = 'rolled_back'", verifyConn);
        var count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CommitAsync_WithoutBegin_DoesNotThrow()
    {
        await using var uow = new UnitOfWork(_dataSource);

        // Should not throw — transaction is null, nothing to commit
        await uow.CommitAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithoutBegin_DoesNotThrow()
    {
        await using var uow = new UnitOfWork(_dataSource);

        // Should not throw — transaction is null, nothing to rollback
        await uow.RollbackAsync();
    }

    [Fact]
    public async Task DisposeAsync_CleansUpConnectionAndTransaction()
    {
        var uow = new UnitOfWork(_dataSource);
        await uow.BeginAsync();

        Assert.NotNull(uow.Connection);
        Assert.NotNull(uow.Transaction);

        await uow.DisposeAsync();

        // After dispose, accessing Connection throws because it was nulled
        Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);
        Assert.Null(uow.Transaction);
    }

    [Fact]
    public async Task DisposeAsync_WithoutBegin_DoesNotThrow()
    {
        var uow = new UnitOfWork(_dataSource);

        // Should not throw — nothing to dispose
        await uow.DisposeAsync();
    }
}
