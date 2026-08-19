using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App.Services;

/// <summary>
/// Checks whether a newer version exists. The actual source (public GitHub
/// releases, a feed of one's own) is deliberately interchangeable.
/// </summary>
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
