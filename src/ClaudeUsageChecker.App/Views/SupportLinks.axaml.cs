using System;
using Avalonia.Automation;
using Avalonia.Controls;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// The two ways to support the project - Buy Me a Coffee and Ko-fi - as the
/// buttons those services hand out.
/// </summary>
/// <remarks>
/// <para>
/// One control for the three places they appear: the menu of the notification
/// area, the about window and the foot of the settings. Kept in one place, the
/// addresses cannot drift apart between them.
/// </para>
/// <para>
/// A click is reported, not acted on - opening a browser stays gathered in the
/// application, as it does for the project page and the release page.
/// </para>
/// </remarks>
public partial class SupportLinks : UserControl
{
    /// <summary>The page on Buy Me a Coffee.</summary>
    public static Uri BuyMeACoffee { get; } = new("https://buymeacoffee.com/svenreichelt");

    /// <summary>The page on Ko-fi.</summary>
    public static Uri Kofi { get; } = new("https://ko-fi.com/svenreichelt");

    public SupportLinks()
    {
        InitializeComponent();

        CoffeeButton.Click += (_, _) => LinkRequested?.Invoke(this, BuyMeACoffee);
        KofiButton.Click += (_, _) => LinkRequested?.Invoke(this, Kofi);

        ApplyTexts();
    }

    /// <summary>A button was clicked; the address is the page to open.</summary>
    public event EventHandler<Uri>? LinkRequested;

    /// <summary>
    /// How tall the buttons are drawn. Their widths follow from the pictures,
    /// which do not share a shape.
    /// </summary>
    public double ImageHeight
    {
        get => CoffeeImage.Height;
        set
        {
            CoffeeImage.Height = value;
            KofiImage.Height = value;
        }
    }

    /// <summary>
    /// Sets the tooltips and the names a screen reader announces. The pictures
    /// carry English words; these say in the chosen language where a click leads.
    /// </summary>
    public void ApplyTexts()
    {
        ToolTip.SetTip(CoffeeButton, T.SupportCoffee);
        ToolTip.SetTip(KofiButton, T.SupportKofi);
        AutomationProperties.SetName(CoffeeButton, T.SupportCoffee);
        AutomationProperties.SetName(KofiButton, T.SupportKofi);
    }
}
