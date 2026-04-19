using Npgsql;

namespace identity_service.Data;

public class UnitOfWork(NpgsqlDataSource dataSource) : IUnitOfWork
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public NpgsqlConnection Connection =>
        _connection ?? throw new InvalidOperationException("Call BeginAsync before accessing Connection.");

    public NpgsqlTransaction? Transaction => _transaction;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        _connection = await dataSource.OpenConnectionAsync(ct);
        _transaction = await _connection.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        GC.SuppressFinalize(this);
    }
}
