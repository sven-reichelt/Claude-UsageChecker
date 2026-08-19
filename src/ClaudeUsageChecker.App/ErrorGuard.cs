using System;
using System.Threading.Tasks;

namespace ClaudeUsageChecker.App;

/// <summary>
/// Wraps actions triggered from the interface, so that a failure in one does not
/// terminate the application.
/// </summary>
/// <remarks>
/// A tray application has no window in which a failure would show: an exception
/// in a menu handler travels all the way to the message loop and ends the
/// process without a word. All the user sees is that the icon has vanished. This
/// guard therefore catches every exception, records it with its context, and
/// lets the application carry on.
/// </remarks>
internal static class ErrorGuard
{
    /// <summary>Runs an action and catches every failure.</summary>
    public static void Run(string context, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, context);
        }
    }

    /// <summary>
    /// Starts an asynchronous action without awaiting it, and catches every
    /// failure. Replaces discarding the task with an underscore.
    /// </summary>
    public static void Forget(string context, Func<Task> action)
    {
        _ = RunAsync(context, action);
    }

    /// <summary>Runs an asynchronous action and catches every failure.</summary>
    public static async Task RunAsync(string context, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            CrashReporter.Write(ex, context);
        }
    }
}
