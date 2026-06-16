namespace Mvec.Identity.Api.Domain;

/// <summary>Join entity for the user↔role many-to-many (idn.UserRoles). Composite key (UserId, RoleId).</summary>
public class UserRole
{
    public long UserId { get; private set; }
    public int RoleId { get; private set; }

    private UserRole() { }

    public static UserRole Create(long userId, int roleId) => new() { UserId = userId, RoleId = roleId };

    /// <summary>For assignment off a tracked/new user — EF fills UserId from the aggregate on save.</summary>
    public static UserRole ForRole(int roleId) => new() { RoleId = roleId };
}
