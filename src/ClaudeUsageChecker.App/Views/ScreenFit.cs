using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Keeps a window that sizes itself to its content within the visible screen.
/// </summary>
/// <remarks>
/// Every window here uses <c>SizeToContent="Height"</c> together with
/// <c>CenterScreen</c>, and that combination has a trap in it: Avalonia centres
/// the window using the height it has at the moment it opens. The content then
/// grows downwards while the position stays where it was. The settings window
/// ended up 124 pixels below the bottom edge that way, taking the save button
/// with it - and a height cap alone did not catch it, because the cap says
/// nothing about where the window sits.
///
/// Longer translations make this worse rather than better: French and Russian
/// run noticeably longer than the English source, so a window that just fits in
/// English can overflow in another language.
/// </remarks>
internal static class ScreenFit
{
    /// <summary>First guess at the room taken by title bar, border and margins.</summary>
    /// <remarks>
    /// Only a starting value. What actually sits outside the scroll area - a
    /// docked button row, for instance - is measured afterwards rather than
    /// guessed, see <see cref="Fit"/>.
    /// </remarks>
    private const double FrameAllowance = 60d;

    /// <summary>Smallest height still worth showing, however low the screen.</summary>
    private const double MinimumContentHeight = 200d;

    /// <summary>
    /// Caps <paramref name="scroller"/> to the screen and moves
    /// <paramref name="window"/> back up if it hangs over the bottom edge.
    /// </summary>
    /// <remarks>
    /// To be called from <c>Opened</c>: the screen is only known once the window
    /// is open.
    /// </remarks>
    public static void Apply(Window window, Control? scroller = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Screens.ScreenFromWindow(window) is not { } screen)
        {
            return;
        }

        if (scroller is not null)
        {
            // WorkingArea counts pixels while the layout works in
            // device-independent units - without dividing by the scaling, the
            // limit comes out far too generous on a high-resolution screen.
            var available = screen.WorkingArea.Height / screen.Scaling;
            Cap(scroller, available - FrameAllowance);
        }

        // Content that arrives after the window is open - the changelog is
        // rendered into it - changes the height once more. A single pass after
        // opening would measure the state before that.
        window.SizeChanged += (_, _) => Fit(window, scroller);

        // The correction has to wait for the layout to run, otherwise the height
        // read is still the one from before the cap took effect.
        PostWhenStillOpen(window, () => Fit(window, scroller));
    }

    /// <summary>
    /// Lowers a limit, never raises it.
    /// </summary>
    /// <remarks>
    /// The value in the XAML is a deliberate decision - the changelog window
    /// starts scrolling at 440 so that a long list does not fill the screen.
    /// Overwriting it with the screen height turned a window that fitted into
    /// one 28 pixels too tall. The job here is to limit on a small screen, not
    /// to grant more room on a large one.
    /// </remarks>
    private static void Cap(Control scroller, double limit)
    {
        var wanted = Math.Max(MinimumContentHeight, limit);

        scroller.MaxHeight = double.IsFinite(scroller.MaxHeight)
            ? Math.Min(scroller.MaxHeight, wanted)
            : wanted;
    }

    /// <summary>
    /// Measures the window once it has been laid out and, if it is still too
    /// tall, takes the excess off the scroll area.
    /// </summary>
    /// <remarks>
    /// Measuring rather than calculating, because what surrounds the scroll area
    /// differs from window to window - the settings window docks its button row
    /// below it so that "save" stays reachable on a low screen, and no fixed
    /// allowance would have known about that.
    /// </remarks>
    private static void Fit(Window window, Control? scroller)
    {
        // SizeChanged also fires while a window is closing, and a closed window
        // has no platform implementation left to ask for its screen.
        if (!window.IsVisible)
        {
            return;
        }

        if (window.Screens.ScreenFromWindow(window) is not { } screen)
        {
            return;
        }

        var available = screen.WorkingArea.Height / screen.Scaling;
        var height = (window.FrameSize ?? window.Bounds.Size).Height;
        var excess = height - available;

        if (scroller is not null && excess > 0.5d && scroller.Bounds.Height > 0d)
        {
            Cap(scroller, scroller.Bounds.Height - excess);

            // Shrinking changes the height, so the position is settled in the
            // next pass rather than on a value that is about to be stale.
            PostWhenStillOpen(window, () => MoveIntoWorkingArea(window));
            return;
        }

        MoveIntoWorkingArea(window);
    }

    /// <summary>
    /// Queues work for after the layout, and drops it if the window has closed
    /// in the meantime.
    /// </summary>
    /// <remarks>
    /// A closed window no longer has a platform implementation, and asking it
    /// for its screen throws. That happens on a dispatcher job, where nothing
    /// catches it - and an uncaught exception there takes the whole application
    /// down, because there is no window left to show it in.
    /// </remarks>
    private static void PostWhenStillOpen(Window window, Action work)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (window.IsVisible)
                {
                    work();
                }
            },
            DispatcherPriority.Loaded);
    }

    /// <summary>Moves a window up if it extends past the bottom of the screen.</summary>
    public static void MoveIntoWorkingArea(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Screens.ScreenFromWindow(window) is not { } screen)
        {
            return;
        }

        var frameHeight = (int)Math.Ceiling((window.FrameSize ?? window.Bounds.Size).Height * screen.Scaling);
        var top = TopInsideWorkingArea(
            screen.WorkingArea.Y,
            screen.WorkingArea.Bottom,
            window.Position.Y,
            frameHeight);

        if (top != window.Position.Y)
        {
            window.Position = new PixelPoint(window.Position.X, top);
        }
    }

    /// <summary>
    /// Works out the topmost edge at which a window of <paramref name="frameHeight"/>
    /// stays inside the working area.
    /// </summary>
    /// <remarks>
    /// Only ever moves upwards, and never above the top edge - a window whose
    /// title bar sits off-screen cannot be moved by hand any more. A window too
    /// tall even so sticks to the top edge and overflows at the bottom; that is
    /// the lesser evil, and the cap on the scroll area should have prevented it.
    ///
    /// Kept apart from the window so that it can be tested without one.
    /// </remarks>
    public static int TopInsideWorkingArea(int workingAreaTop, int workingAreaBottom, int currentTop, int frameHeight)
    {
        if (currentTop + frameHeight <= workingAreaBottom)
        {
            return currentTop;
        }

        return Math.Max(workingAreaTop, workingAreaBottom - frameHeight);
    }
}
