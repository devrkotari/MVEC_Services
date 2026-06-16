namespace Mvec.Identity.Api.Domain;

/// <summary>Account category, stored in idn.Users.UserType (VARCHAR).</summary>
public enum UserType
{
    Buyer = 0,
    Vendor = 1,
    Admin = 2
}

/// <summary>Account status, stored in idn.Users.Status (VARCHAR).</summary>
public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Deleted = 2
}

/// <summary>OTP purpose, stored in idn.OtpCodes.Purpose (VARCHAR).</summary>
public enum OtpPurpose
{
    EmailVerify = 0,
    PhoneVerify = 1,
    PasswordReset = 2
}
