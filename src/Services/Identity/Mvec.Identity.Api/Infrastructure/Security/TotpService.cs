using System.Web;
using Mvec.Identity.Api.Application.Abstractions;
using OtpNet;

namespace Mvec.Identity.Api.Infrastructure.Security;

/// <summary>TOTP (RFC 6238) implementation for admin 2FA, backed by Otp.NET.</summary>
public sealed class TotpService : ITotpService
{
    // Allow one step before/after to tolerate clock drift between server and authenticator.
    private static readonly VerificationWindow Window = new(previous: 1, future: 1);

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160-bit, per RFC 4226
        return Base32Encoding.ToString(key);
    }

    public bool Verify(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var totp = new Totp(Base32Encoding.ToBytes(base32Secret));
            return totp.VerifyTotp(code.Trim(), out _, Window);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string BuildProvisioningUri(string base32Secret, string accountName, string issuer)
    {
        var label = HttpUtility.UrlEncode($"{issuer}:{accountName}");
        var enc = HttpUtility.UrlEncode(issuer);
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={enc}&algorithm=SHA1&digits=6&period=30";
    }
}
