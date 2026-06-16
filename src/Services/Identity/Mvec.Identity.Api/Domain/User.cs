namespace Mvec.Identity.Api.Domain;

/// <summary>
/// Aggregate root mapped to <c>idn.Users</c>. Keys are database-generated (BIGINT IDENTITY).
/// Owns its refresh tokens, external logins and role assignments.
/// </summary>
public class User
{
    public long Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }            // null for social-only accounts
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserType UserType { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public bool EmailConfirmed { get; private set; }
    public bool PhoneConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { }

    private User(string email, string firstName, string lastName, UserType userType, string? phoneNumber)
    {
        Email = email.Trim().ToLowerInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        PhoneNumber = phoneNumber;
    }

    public static User CreateLocal(string email, string passwordHash, string firstName, string lastName,
        UserType userType, string? phoneNumber = null)
    {
        return new User(email, firstName, lastName, userType, phoneNumber) { PasswordHash = passwordHash };
    }

    public static User CreateExternal(string email, string firstName, string lastName, UserType userType,
        string provider, string providerKey)
    {
        var user = new User(email, firstName, lastName, userType, null) { EmailConfirmed = true };
        user.AddExternalLogin(provider, providerKey);
        return user;
    }

    public bool IsActive => Status == UserStatus.Active;
    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);
    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed) return;
        EmailConfirmed = true;
        Touch();
    }

    public void RecordLogin()
    {
        LastLoginUtc = DateTime.UtcNow;
        AccessFailedCount = 0;
    }

    public void Suspend()
    {
        if (Status == UserStatus.Suspended) return;
        Status = UserStatus.Suspended;
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();
        Touch();
    }

    public void Reinstate()
    {
        if (Status == UserStatus.Active) return;
        Status = UserStatus.Active;
        Touch();
    }

    public void ChangeUserType(UserType userType)
    {
        if (UserType == userType) return;
        UserType = userType;
        Touch();
    }

    public void EnableTwoFactor(string base32Secret)
    {
        TwoFactorSecret = base32Secret;
        TwoFactorEnabled = true;
        Touch();
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        Touch();
    }

    public RefreshToken IssueRefreshToken(byte[] tokenHash, DateTime expiresUtc)
    {
        var token = RefreshToken.Issue(Id, tokenHash, expiresUtc);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>Links an external provider account. Returns the new link, or null if already present.</summary>
    public ExternalLogin? AddExternalLogin(string provider, string providerKey)
    {
        if (_externalLogins.Any(l => l.Provider == provider && l.ProviderKey == providerKey)) return null;
        var login = ExternalLogin.Create(Id, provider, providerKey);
        _externalLogins.Add(login);
        return login;
    }

    /// <summary>Assigns a role (idn.UserRoles). UserId is set by EF from the aggregate on save.</summary>
    public void AssignRole(int roleId)
    {
        if (_userRoles.Any(r => r.RoleId == roleId)) return;
        _userRoles.Add(UserRole.ForRole(roleId));
    }

    private void Touch() => UpdatedUtc = DateTime.UtcNow;
}
