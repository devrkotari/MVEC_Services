using Microsoft.Extensions.Options;
using Mvec.BuildingBlocks.Persistence;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Options;
using Mvec.Identity.Api.Domain;

namespace Mvec.Identity.Api.Infrastructure.Seed;

/// <summary>
/// Seeds reference data (roles) and the bootstrap admin account. Idempotent.
/// Database-first: does NOT create or migrate schema — the idn.* tables must already exist.
/// </summary>
public sealed class IdentitySeeder
{
    private static readonly string[] RoleNames = ["Buyer", "Vendor", "Admin"];

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totp;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        IUserRepository users,
        IRoleRepository roles,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITotpService totp,
        IOptions<AdminSeedOptions> options,
        ILogger<IdentitySeeder> logger)
    {
        _users = users;
        _roles = roles;
        _uow = uow;
        _passwordHasher = passwordHasher;
        _totp = totp;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedAdminAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var existing = (await _roles.GetAllAsync(ct)).Select(r => r.Name).ToHashSet();
        var added = false;
        foreach (var name in RoleNames.Where(n => !existing.Contains(n)))
        {
            _roles.Add(Role.Create(name));
            added = true;
        }
        if (added) await _uow.SaveChangesAsync(ct);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var email = _options.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, ct))
        {
            _logger.LogInformation("Admin account {Email} already present; skipping seed.", email);
            return;
        }

        var admin = User.CreateLocal(
            email, _passwordHasher.Hash(_options.Password), _options.FirstName, _options.LastName, UserType.Admin);
        admin.ConfirmEmail();

        if (_options.EnableTwoFactor)
        {
            var secret = string.IsNullOrWhiteSpace(_options.TwoFactorSecret)
                ? _totp.GenerateSecret()
                : _options.TwoFactorSecret!;
            admin.EnableTwoFactor(secret);

            if (string.IsNullOrWhiteSpace(_options.TwoFactorSecret))
                _logger.LogWarning(
                    "Seeded admin TOTP secret (store securely, shown once): {Secret} | otpauth: {Uri}",
                    secret, _totp.BuildProvisioningUri(secret, email, "MVEC"));
        }

        var adminRole = await _roles.GetByNameAsync(UserType.Admin.ToString(), ct);
        if (adminRole is not null) admin.AssignRole(adminRole.Id);

        _users.Add(admin);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded admin account {Email}.", email);
    }
}
