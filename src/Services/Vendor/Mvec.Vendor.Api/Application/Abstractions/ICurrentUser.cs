namespace Mvec.Vendor.Api.Application.Abstractions;

/// <summary>Accessor for the authenticated principal (from the JWT) on the current request.</summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user id (JWT <c>sub</c>) — the vendor's owning user.</summary>
    long? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
