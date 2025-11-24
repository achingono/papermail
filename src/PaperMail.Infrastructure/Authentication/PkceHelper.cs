using System.Security.Cryptography;
using System.Text;

namespace PaperMail.Infrastructure.Authentication;

/// <summary>
/// Helper for generating PKCE (Proof Key for Code Exchange) parameters.
/// Used to secure OAuth 2.0 authorization code flow.
/// </summary>
public static class PkceHelper
{
    /// <summary>
    /// Generates a cryptographically secure code verifier (43-128 characters).
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Base64UrlEncode(randomBytes);
    }

    /// <summary>
    /// Generates code challenge from verifier using SHA256.
    /// </summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    /// <summary>
    /// Base64-URL encoding without padding (per RFC 7636).
    /// </summary>
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
