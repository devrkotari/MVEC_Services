using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mvec.Vendor.Api.Application.Abstractions;
using Mvec.Vendor.Api.Application.Services;
using Mvec.Vendor.Api.Infrastructure;
using Mvec.Vendor.Api.Infrastructure.Repositories;

namespace Mvec.Vendor.Tests;

/// <summary>Captures published integration events for assertions.</summary>
public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<object> Published { get; } = new();

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Wires the real Vendor services + repositories + UnitOfWork over a SQLite in-memory database.
/// SQLite (rather than the EF in-memory provider) is used so VARBINARY columns and relational
/// behaviour (cascades, unique constraints) match production.
/// </summary>
public sealed class VendorTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    public VendorDbContext Db { get; }
    public VendorService Vendors { get; }
    public StoreService Stores { get; }
    public RecordingEventPublisher Events { get; } = new();

    public VendorTestHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<VendorDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new VendorDbContext(options);
        Db.Database.EnsureCreated();

        var vendorRepo = new VendorRepository(Db);
        var storeRepo = new VendorStoreRepository(Db);

        Vendors = new VendorService(vendorRepo, Db, Events);
        Stores = new StoreService(storeRepo, vendorRepo, Db);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
