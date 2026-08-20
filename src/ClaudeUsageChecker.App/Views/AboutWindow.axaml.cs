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
        CloseButton.Click += (_, _) => Close();
    }

    /// <summary>Sets every fixed label from the language file.</summary>
    private void ApplyTexts(ProgramVersion version)
    {
        Title = T.AboutTitle;
        VersionText.Text = T.Version(version.ToString());
        DescriptionText.Text = T.AboutDescription;
        RepositoryButton.Content = T.AboutRepository;
        ReleaseNotesButton.Content = T.AboutReleaseNotes;
        LicenseText.Text = T.AboutLicense(Copyright());
        CloseButton.Content = T.Close;
    }

    /// <summary>The user wants to open the project page in a browser.</summary>
    public event EventHandler<Uri>? RepositoryRequested;

    /// <summary>The user wants to see the changelog.</summary>
    public event EventHandler? ReleaseNotesRequested;

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
