using Avalonia.Styling;

namespace ClaudeUsageChecker.App.Settings;

/// <summary>Light, dark, or whatever the system is set to.</summary>
public enum AppearanceMode
{
    /// <summary>Follow the operating system. What most people want.</summary>
    System,

    Light,

    Dark
}

/// <summary>Turns the choice into what Avalonia understands.</summary>
public static class AppearanceModes
{
    /// <summary>
    /// The theme variant for a choice.
    /// </summary>
    /// <remarks>
    /// <see cref="ThemeVariant.Default"/> is not a third colour - it means
    /// "ask the system", which is what following it amounts to.
    /// </remarks>
    public static ThemeVariant ToVariant(this AppearanceMode mode) => mode switch
    {
        AppearanceMode.Light => ThemeVariant.Light,
        AppearanceMode.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };

    /// <summary>
    /// Reads the stored text. Anything unreadable counts as following the
    /// system - a corrupted settings file must not leave the application in a
    /// colour scheme nobody chose.
    /// </summary>
    public static AppearanceMode Parse(string? stored) => stored?.ToLowerInvariant() switch
    {
        "light" => AppearanceMode.Light,
        "dark" => AppearanceMode.Dark,
        _ => AppearanceMode.System
    };

    /// <summary>Writes it down, in a form that stays readable in the file.</summary>
    public static string Format(AppearanceMode mode) => mode switch
    {
        AppearanceMode.Light => "light",
        AppearanceMode.Dark => "dark",
        _ => "system"
    };
}
