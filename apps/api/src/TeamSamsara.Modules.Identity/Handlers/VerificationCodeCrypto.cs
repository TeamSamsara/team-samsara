// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/VerificationCodeCrypto.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The cryptographic pieces of verification codes: random generation, salting,
// hashing and constant-time comparison. Kept apart from the service so the service reads as
// workflow, not cryptography.

using System.Security.Cryptography;
using System.Text;

namespace TeamSamsara.Modules.Identity.Handlers;

internal static class VerificationCodeCrypto
{
    #region Fields

    private const int DecimalDigitBase = 10;
    private const int SaltSizeBytes = 16;

    #endregion

    #region Public Methods

    // A random numeric code of the given length (leading zeros allowed)
    public static string GenerateCode(int length)
    {
        var digits = new char[length];

        for (var i = 0; i < length; i++)
        {
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(DecimalDigitBase));
        }

        return new string(digits);
    }

    // A fresh random salt, base64 encoded
    public static string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeBytes));
    }

    // SHA-256 over the salt followed by the code, base64 encoded
    public static string Hash(string code, string base64Salt)
    {
        return Convert.ToBase64String(ComputeHash(code, Convert.FromBase64String(base64Salt)));
    }

    // Whether a submitted code matches a stored hash, compared in constant time
    public static bool Matches(string submittedCode, string base64Salt, string base64ExpectedHash)
    {
        var submittedHash = ComputeHash(submittedCode, Convert.FromBase64String(base64Salt));
        var expectedHash = Convert.FromBase64String(base64ExpectedHash);

        return CryptographicOperations.FixedTimeEquals(submittedHash, expectedHash);
    }

    #endregion

    #region Private Methods

    private static byte[] ComputeHash(string code, byte[] salt)
    {
        var codeBytes = Encoding.UTF8.GetBytes(code);
        var input = new byte[salt.Length + codeBytes.Length];

        salt.CopyTo(input, 0);
        codeBytes.CopyTo(input, salt.Length);

        return SHA256.HashData(input);
    }

    #endregion
}
