using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Platform;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// The four icons of the notification area, as an Avalonia image or as a
/// Windows icon handle.
/// </summary>
/// <remarks>
/// The images live as PNG resources inside the single file, so there is no path
/// to hand to LoadImage. CreateIconFromResourceEx takes the bytes as they are -
/// PNG included, which Windows has understood since Vista - and that is what
/// makes it possible to register the icon without unpacking anything to disk
/// first.
/// </remarks>
internal static class TrayIconImages
{
    private const uint IconVersion = 0x00030000;

    public static Uri UriFor(TrayIconSeverity severity) =>
        new($"avares://ClaudeUsageChecker/Assets/{Name(severity)}.png");

    public static WindowIcon Load(TrayIconSeverity severity) =>
        new(AssetLoader.Open(UriFor(severity)));

    /// <summary>
    /// Builds a Windows icon handle. The caller owns it and has to destroy it.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IntPtr CreateHandle(TrayIconSeverity severity)
    {
        using var stream = AssetLoader.Open(UriFor(severity));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        // The size Windows wants for the notification area - it differs with the
        // scaling, and a wrong one is scaled by Windows, which looks soft.
        var width = GetSystemMetrics(SM_CXSMICON);
        var height = GetSystemMetrics(SM_CYSMICON);

        return CreateIconFromResourceEx(
            bytes, (uint)bytes.Length, fIcon: true, IconVersion, width, height, 0);
    }

    private static string Name(TrayIconSeverity severity) => severity switch
    {
        TrayIconSeverity.Warning => "tray-warning",
        TrayIconSeverity.Critical => "tray-critical",
        TrayIconSeverity.Inactive => "tray-inactive",
        _ => "tray-normal"
    };

    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconFromResourceEx(
        byte[] presbits, uint dwResSize, [MarshalAs(UnmanagedType.Bool)] bool fIcon,
        uint dwVer, int cxDesired, int cyDesired, uint flags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
