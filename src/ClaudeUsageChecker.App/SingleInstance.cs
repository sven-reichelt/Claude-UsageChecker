using System;
using System.Threading;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Makes sure the application runs only once per login session.
/// </summary>
/// <remarks>
/// Without this lock every further start adds another tray icon, and each
/// instance polls the API on its own - which burdens the throttle-sensitive
/// endpoint for nothing.
/// The name carries the "Local\" prefix so that the lock applies per login
/// session and several users on the same machine do not lock each other out.
/// </remarks>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\ClaudeUsageChecker.SingleInstance";

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>
    /// Tries to take the lock. Returns null when an instance is already running.
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: false, MutexName);

        try
        {
            if (!mutex.WaitOne(TimeSpan.Zero, exitContext: false))
            {
                mutex.Dispose();
                return null;
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous instance was killed. The lock is ours now.
        }

        return new SingleInstance(mutex);
    }

    public void Dispose()
    {
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The lock was no longer held - irrelevant during shutdown.
        }

        _mutex.Dispose();
    }
}
