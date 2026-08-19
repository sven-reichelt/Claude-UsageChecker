using System;
using System.Threading;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Stellt sicher, dass die Anwendung nur einmal je Benutzersitzung laeuft.
/// </summary>
/// <remarks>
/// Ohne diese Sperre erscheint bei jedem weiteren Start ein zusaetzliches Symbol
/// im Infobereich, und jede Instanz fragt die API eigenstaendig ab - was den
/// drosselungsempfindlichen Endpunkt unnoetig belastet.
/// Der Name traegt das Praefix "Local\", damit die Sperre je Anmeldesitzung gilt
/// und mehrere Nutzer auf demselben Rechner sich nicht gegenseitig aussperren.
/// </remarks>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\ClaudeUsageChecker.SingleInstance";

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>
    /// Versucht, die Sperre zu belegen. Liefert null, wenn bereits eine Instanz laeuft.
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
            // Die vorherige Instanz wurde hart beendet. Die Sperre gehoert nun uns.
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
            // Die Sperre war nicht mehr belegt - beim Beenden unerheblich.
        }

        _mutex.Dispose();
    }
}
