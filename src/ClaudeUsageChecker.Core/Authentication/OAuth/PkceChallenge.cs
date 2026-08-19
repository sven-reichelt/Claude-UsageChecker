using System.Security.Cryptography;
using System.Text;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Ein Paar aus Verifier und Challenge nach RFC 7636 (PKCE, Verfahren S256).
/// </summary>
/// <remarks>
/// PKCE bindet den spaeteren Tausch des Autorisierungscodes an genau den
/// Vorgang, der ihn angefordert hat. Ohne das koennte ein abgefangener Code von
/// einem Dritten eingeloest werden - bei einer Anwendung ohne Client-Geheimnis
/// die entscheidende Absicherung.
/// </remarks>
public sealed class PkceChallenge
{
    private PkceChallenge(string verifier, string challenge)
    {
        Verifier = verifier;
        Challenge = challenge;
    }

    /// <summary>Das Geheimnis, das erst beim Tausch mitgeschickt wird.</summary>
    public string Verifier { get; }

    /// <summary>Der oeffentliche Abdruck des Verifiers, base64url-kodiert.</summary>
    public string Challenge { get; }

    public const string Method = "S256";

    /// <summary>Erzeugt ein frisches Paar mit 32 Byte Zufall.</summary>
    public static PkceChallenge Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return new PkceChallenge(verifier, challenge);
    }

    /// <summary>Baut ein Paar aus einem bekannten Verifier - nur fuer Tests.</summary>
    internal static PkceChallenge FromVerifier(string verifier) =>
        new(verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));

    /// <summary>base64url ohne Auffuellzeichen, wie von RFC 7636 verlangt.</summary>
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
