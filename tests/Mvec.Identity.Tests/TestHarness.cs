using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Options;
using Mvec.Identity.Api.Application.Services;
using Mvec.Identity.Api.Domain;
using Mvec.Identity.Api.Infrastructure;
using Mvec.Identity.Api.Infrastructure.Repositories;
using Mvec.Identity.Api.Infrastructure.Security;

namespace Mvec.Identity.Tests;

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
/// Builds an <see cref="AuthService"/> backed by a real (SQLite in-memory) relational database.
/// SQLite is used instead of the EF in-memory provider because refresh/OTP lookups compare
/// VARBINARY hashes by value — which the in-memory provider does not support.
/// </summary>
public sealed class AuthTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    public IdentityDbContext Db { get; }
    public AuthService Auth { get; }
    public UserService Users { get; }
    public AddressService Addresses { get; }
    public RoleService Roles { get; }
    public RecordingEventPublisher Events { get; } = new();
    public ITotpService Totp { get; } = new TotpService();
    public IJwtTokenService Jwt { get; }

    private static readonly JwtOptions JwtOptions = new()
    {
        Issuer = "mvec-test",
        Audience = "mvec-test",
        SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    public AuthTestHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new IdentityDbContext(options);
        Db.Database.EnsureCreated();

        // Reference data: roles must exist so registration can assign the matching one.
        Db.Roles.AddRange(Role.Create("Buyer"), Role.Create("Vendor"), Role.Create("Admin"));
        Db.SaveChanges();

        Jwt = new JwtTokenService(Options.Create(JwtOptions));

        var userRepo = new UserRepository(Db);
        var roleRepo = new RoleRepository(Db);
        var userRoleRepo = new UserRoleRepository(Db);

        Auth = new AuthService(
            userRepo,
            roleRepo,
            new RefreshTokenRepository(Db),
            new ExternalLoginRepository(Db),
            new OtpRepository(Db),
            Db, // IUnitOfWork
            new Pbkdf2PasswordHasher(),
            new Sha256TokenHasher(),
            Jwt,
            Totp,
            Events,
            externalValidators: Array.Empty<IExternalAuthValidator>(),
            NullLogger<AuthService>.Instance);

        Users = new UserService(userRepo, roleRepo, userRoleRepo, Db);
        Addresses = new AddressService(new UserAddressRepository(Db), Db);
        Roles = new RoleService(roleRepo);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
