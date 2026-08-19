using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Prueft, ob eine neuere Version vorliegt. Die konkrete Bezugsquelle
/// (oeffentliche GitHub-Releases, eigener Feed) ist bewusst austauschbar.
/// </summary>
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
