// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/PasswordResetCrypto.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Hashing and token generation for password reset.

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace TeamSamsara.Modules.Identity.Handlers;

public static class PasswordResetCrypto
{
    #region Fields

    private const int TokenSizeBytes = 32;

    #endregion

    #region Public Methods

    // SHA-256 hash of the normalized email address.
    public static string HashEmail(string email)
    {
        return Sha256Hex(email.Trim().ToLowerInvariant());
    }

    // Random URL-safe token, returned to the member once.
    public static string GenerateToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
    }

    // SHA-256 hash of the token; only the hash is stored.
    public static string HashToken(string token)
    {
        return Sha256Hex(token);
    }

    #endregion

    #region Private Methods

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    #endregion
}
