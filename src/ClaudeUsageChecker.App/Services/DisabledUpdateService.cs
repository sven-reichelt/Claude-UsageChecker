using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Platzhalter, solange das Repository privat ist: GitHub-Releases sind ohne
/// Zugriffstoken nicht abrufbar. Wird das Repository oeffentlich, genuegt es,
/// stattdessen den <see cref="GitHubReleaseUpdateService"/> zu registrieren.
/// </summary>
public sealed class DisabledUpdateService : IUpdateService
{
    public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(UpdateCheckResult.Disabled(
            "Die Aktualisierungspruefung ist noch nicht aktiv. Sie wird freigeschaltet, " +
            "sobald die Veroeffentlichungen oeffentlich zugaenglich sind."));
}
