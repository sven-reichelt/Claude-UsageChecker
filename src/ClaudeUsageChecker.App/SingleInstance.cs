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
    /// <summary>
    /// The name of the lock. Windows wants the "Local\" prefix for a lock that
    /// applies per login session; on macOS and Linux the name becomes a file
    /// name, where a backslash has no such meaning and only makes trouble.
    /// </summary>
    internal static string MutexName => OperatingSystem.IsWindows()
        ? @"Local\ClaudeUsageChecker.SingleInstance"
        : "ClaudeUsageChecker.SingleInstance";

    private readonly Mutex? _mutex;

    private SingleInstance(Mutex? mutex) => _mutex = mutex;

    /// <summary>
    /// Tries to take the lock. Returns null when an instance is already running.
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        Mutex mutex;

        try
        {
            mutex = new Mutex(initiallyOwned: false, MutexName);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Named locks are not available everywhere, and where they are the
            // rules about names differ. Going without one costs a second icon
            // if the application is started twice; refusing to start over it
            // would cost the application altogether. Note it and carry on.
            CrashReporter.Write(ex, "SingleInstance.TryAcquire");
            return new SingleInstance(mutex: null);
        }

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
        if (_mutex is null)
        {
            return;
        }

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
