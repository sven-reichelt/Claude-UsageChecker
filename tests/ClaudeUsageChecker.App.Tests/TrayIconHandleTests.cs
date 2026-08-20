using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using ClaudeUsageChecker.App.Tray;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that a Windows icon handle can be built from the icons the project
/// generates.
/// </summary>
/// <remarks>
/// The tray icon is registered through Shell_NotifyIcon, which wants an HICON.
/// The icons themselves live as PNG resources inside the single file, so there
/// is no path to hand to LoadImage. CreateIconFromResourceEx takes the bytes
/// directly - PNG included, since Vista - and that is the whole reason the
/// approach is viable. If it were not, the icon would have to be unpacked to a
/// temporary file on every start.
/// </remarks>
public class TrayIconHandleTests
{
    // Windows only, like the notification area itself. The project builds and
    // runs on Windows; a run elsewhere should say so loudly rather than skip.
    [SupportedOSPlatform("windows")]
    [AvaloniaTheory]
    [InlineData(TrayIconSeverity.Normal)]
    [InlineData(TrayIconSeverity.Warning)]
    [InlineData(TrayIconSeverity.Critical)]
    [InlineData(TrayIconSeverity.Inactive)]
    public void EveryStateYieldsAnIconHandle(TrayIconSeverity severity)
    {
        var handle = TrayIconImages.CreateHandle(severity);

        Assert.NotEqual(IntPtr.Zero, handle);

        DestroyIcon(handle);
    }

    /// <summary>The resources are actually there, whatever the platform.</summary>
    [AvaloniaTheory]
    [InlineData(TrayIconSeverity.Normal)]
    [InlineData(TrayIconSeverity.Inactive)]
    public void EveryStateHasAnImage(TrayIconSeverity severity)
    {
        using var stream = AssetLoader.Open(TrayIconImages.UriFor(severity));

        Assert.True(stream.Length > 0);
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
