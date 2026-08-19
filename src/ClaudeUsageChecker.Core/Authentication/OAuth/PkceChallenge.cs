using System.Security.Cryptography;
using System.Text;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// A verifier and challenge pair per RFC 7636 (PKCE, method S256).
/// </summary>
/// <remarks>
/// PKCE ties the later exchange of the authorization code to exactly the flow
/// that requested it. Without it, an intercepted code could be redeemed by a
/// third party - the decisive safeguard for an application without a client
/// secret.
/// </remarks>
public sealed class PkceChallenge
{
    private PkceChallenge(string verifier, string challenge)
    {
        Verifier = verifier;
        Challenge = challenge;
    }

    /// <summary>The secret that is sent only when the code is exchanged.</summary>
    public string Verifier { get; }

    /// <summary>The public digest of the verifier, base64url encoded.</summary>
    public string Challenge { get; }

    public const string Method = "S256";

    /// <summary>Creates a fresh pair from 32 random bytes.</summary>
    public static PkceChallenge Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return new PkceChallenge(verifier, challenge);
    }

    /// <summary>Builds a pair from a known verifier - for tests only.</summary>
    internal static PkceChallenge FromVerifier(string verifier) =>
        new(verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));

    /// <summary>base64url without padding, as RFC 7636 demands.</summary>
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
