using System.Security.Cryptography;
using System.Text;
using Mvec.Identity.Api.Application.Abstractions;

namespace Mvec.Identity.Api.Infrastructure.Security;

/// <summary>
/// Deterministic SHA-256 hash for high-entropy secrets (refresh tokens, OTP codes), stored as
/// VARBINARY. Deterministic so values can be looked up by hash; safe because the inputs are
/// already cryptographically random rather than low-entropy passwords.
/// </summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public byte[] Hash(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);
        return SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }
}
