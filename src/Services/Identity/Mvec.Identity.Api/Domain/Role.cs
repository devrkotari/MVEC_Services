namespace Mvec.Identity.Api.Domain;

/// <summary>RBAC role (idn.Roles). Reference data, seeded by the app. Key is INT IDENTITY.</summary>
public class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Role() { }

    public static Role Create(string name) => new() { Name = name };
}
