using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The two support buttons: where they lead, that their pictures load, and that
/// each of the three places passes a click on.
/// </summary>
public class SupportLinksTests
{
    /// <summary>A typo here sends a supporter to somebody else's page, or nowhere.</summary>
    [Fact]
    public void TheAddressesAreTheProjectOwnersPages()
    {
        Assert.Equal("https://buymeacoffee.com/svenreichelt", SupportLinks.BuyMeACoffee.ToString());
        Assert.Equal("https://ko-fi.com/svenreichelt", SupportLinks.Kofi.ToString());
    }

    [AvaloniaFact]
    public void EachButtonLeadsToItsOwnPage()
    {
        var links = new SupportLinks();
        var requested = new List<Uri>();
        links.LinkRequested += (_, address) => requested.Add(address);

        Click(links, "CoffeeButton");
        Click(links, "KofiButton");

        Assert.Equal([SupportLinks.BuyMeACoffee, SupportLinks.Kofi], requested);
    }

    /// <summary>
    /// A picture that fails to load leaves a button of no size - clickable by
    /// nobody, and noticed by nobody either.
    /// </summary>
    [AvaloniaFact]
    public void BothPicturesLoad()
    {
        var window = new Window { Content = new SupportLinks() };
        window.Show();

        var images = window.GetLogicalDescendants().OfType<Image>().ToList();

        Assert.Equal(2, images.Count);
        Assert.All(images, image =>
        {
            Assert.NotNull(image.Source);
            Assert.True(image.Bounds.Width > image.Bounds.Height * 2,
                $"{image.Name} is {image.Bounds.Width:0} x {image.Bounds.Height:0}, not a button's shape.");
        });
        Assert.IsType<DrawingImage>(images.Single(i => i.Name == "CoffeeImage").Source);

        window.Close();
    }

    [AvaloniaFact]
    public void EveryButtonSaysWhereItLeads()
    {
        var links = new SupportLinks();

        Assert.False(string.IsNullOrWhiteSpace(ToolTip.GetTip(links.FindControl<Button>("CoffeeButton")!) as string));
        Assert.False(string.IsNullOrWhiteSpace(ToolTip.GetTip(links.FindControl<Button>("KofiButton")!) as string));
    }

    [AvaloniaFact]
    public void TheAboutWindowPassesAClickOn()
    {
        var window = new AboutWindow(new Uri("https://example.invalid/repo"), new ProgramVersion(new Version(0, 9, 1)));
        Uri? requested = null;
        window.SupportRequested += (_, address) => requested = address;

        Click(window.FindControl<SupportLinks>("Support")!, "KofiButton");

        Assert.Equal(SupportLinks.Kofi, requested);
    }

    [AvaloniaFact]
    public void TheSettingsWindowPassesAClickOn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cuc-test-{Guid.NewGuid():N}.json");
        var window = new SettingsWindow(new SettingsStore(path), new AppSettings(), applyAutostart: _ => { });
        Uri? requested = null;
        window.SupportRequested += (_, address) => requested = address;

        Click(window.FindControl<SupportLinks>("Support")!, "CoffeeButton");

        Assert.Equal(SupportLinks.BuyMeACoffee, requested);
        Assert.False(File.Exists(path));
    }

    /// <summary>Like every other entry, the menu goes away before the page opens.</summary>
    [AvaloniaFact]
    public void TheMenuPassesAClickOnAndCloses()
    {
        var menu = LayoutInEveryLanguageTests.BuildTrayMenu();
        Uri? requested = null;
        menu.SupportRequested += (_, address) => requested = address;
        menu.Show();

        Click(menu.FindControl<SupportLinks>("Support")!, "KofiButton");

        Assert.Equal(SupportLinks.Kofi, requested);
        Assert.False(menu.IsVisible);
    }

    private static void Click(Control owner, string name) =>
        owner.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
