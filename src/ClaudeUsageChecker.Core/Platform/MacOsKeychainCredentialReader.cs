using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Liest die CLI-Anmeldedaten aus dem macOS-Schluesselbund
/// (Dienst "Claude Code-credentials") ueber das Systemwerkzeug /usr/bin/security.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacOsKeychainCredentialReader(string serviceName = "Claude Code-credentials")
    : IClaudeCliCredentialReader
{
    public const string DefaultServiceName = "Claude Code-credentials";

    public async ValueTask<string?> ReadRawAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("/usr/bin/security")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("find-generic-password");
        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add(serviceName);
        startInfo.ArgumentList.Add("-w");

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            // Exit-Code 44 bedeutet schlicht "Eintrag nicht gefunden".
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
