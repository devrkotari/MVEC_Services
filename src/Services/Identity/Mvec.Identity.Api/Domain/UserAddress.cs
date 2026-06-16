namespace Mvec.Identity.Api.Domain;

/// <summary>A user's postal address (idn.UserAddresses). Key is BIGINT IDENTITY.</summary>
public class UserAddress
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string PostalCode { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "IN";
    public string? Phone { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    private UserAddress() { }

    public static UserAddress Create(long userId, string line1, string? line2, string city, string? state,
        string postalCode, string countryCode, string? phone, bool isDefault) => new()
    {
        UserId = userId,
        Line1 = line1,
        Line2 = line2,
        City = city,
        State = state,
        PostalCode = postalCode,
        CountryCode = countryCode,
        Phone = phone,
        IsDefault = isDefault
    };

    public void Update(string line1, string? line2, string city, string? state,
        string postalCode, string countryCode, string? phone)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        CountryCode = countryCode;
        Phone = phone;
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
}
