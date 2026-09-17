using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Shows where the application comes from and which version it is.
/// </summary>
/// <remarks>
/// The repository address is not opened here but reported as an event -
/// starting foreign programs stays gathered in one place, as it already is for
/// the link to the release page.
/// </remarks>
public partial class AboutWindow : Window
{
    private readonly Uri _repository;

    public AboutWindow() : this(App.RepositoryUri, App.CurrentVersion)
    {
    }

    public AboutWindow(Uri repository, ProgramVersion version)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(version);

        _repository = repository;

        InitializeComponent();

        // Longer translations grow the window downwards; without this it
        // can end up reaching past the bottom edge of the screen.
        Opened += (_, _) => ScreenFit.Apply(this);

        LogoImage.Source = LoadLogo();
        RepositoryText.Text = repository.Host + repository.AbsolutePath;
        ApplyTexts(version);

        RepositoryButton.Click += (_, _) => RepositoryRequested?.Invoke(this, _repository);
        ReleaseNotesButton.Click += (_, _) => ReleaseNotesRequested?.Invoke(this, EventArgs.Empty);
        GuideButton.Click += (_, _) =>
            GuideRequested?.Invoke(this, GuideAddress(_repository, Localizer.Current.Language));
        Support.LinkRequested += (_, address) => SupportRequested?.Invoke(this, address);
        CloseButton.Click += (_, _) => Close();
    }

    /// <summary>Sets every fixed label from the language file.</summary>
    private void ApplyTexts(ProgramVersion version)
    {
        Title = T.AboutTitle;
        VersionText.Text = version.IsPreRelease
            ? T.VersionPreRelease(version.ToString())
            : T.Version(version.ToString());
        DescriptionText.Text = T.AboutDescription;
        RepositoryButton.Content = T.AboutRepository;
        ReleaseNotesButton.Content = T.AboutReleaseNotes;
        GuideButton.Content = T.AboutGuide;
        LicenseText.Text = T.AboutLicense(Copyright());
        CloseButton.Content = T.Close;
        SupportIntroText.Text = T.SupportIntro;
    }

    /// <summary>The user wants to support the project; the address is the page to open.</summary>
    public event EventHandler<Uri>? SupportRequested;

    /// <summary>The user wants to open the project page in a browser.</summary>
    public event EventHandler<Uri>? RepositoryRequested;

    /// <summary>The user wants to see the changelog.</summary>
    public event EventHandler? ReleaseNotesRequested;

    /// <summary>The user wants to read the user guide; the address is in their language.</summary>
    public event EventHandler<Uri>? GuideRequested;

    /// <summary>
    /// The user guide in the given language, on the project page.
    /// </summary>
    /// <remarks>
    /// Every language of the interface has a guide of its own, named by the same
    /// code as its language file - docs/guide/de.md, docs/guide/pt-BR.md - so the
    /// address follows from the language without a table to keep in step.
    /// <c>GuideTests</c> checks that each of those files exists.
    /// </remarks>
    internal static Uri GuideAddress(Uri repository, Language language)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(language);

        return new Uri($"{repository.ToString().TrimEnd('/')}/blob/main/docs/guide/{language.Code}.md");
    }

    private static Bitmap? LoadLogo()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://ClaudeUsageChecker/Assets/app.png"));
            return new Bitmap(stream);
        }
        catch (Exception ex) when (ex is System.IO.FileNotFoundException or System.IO.IOException)
        {
            // Without the image the window stays usable.
            return null;
        }
    }

    /// <summary>
    /// Reads the copyright notice from the assembly, so that it is maintained in
    /// one place only - in Directory.Build.props.
    /// </summary>
    private static string Copyright() =>
        typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
        ?? string.Empty;
}
