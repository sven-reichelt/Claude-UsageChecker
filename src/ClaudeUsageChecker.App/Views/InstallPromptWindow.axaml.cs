using System;
using Avalonia.Controls;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Views;

/// <summary>
/// Fragt beim ersten Start, ob die Anwendung sich dauerhaft einrichten soll.
/// </summary>
/// <remarks>
/// Die Frage wird genau einmal gestellt. Wer ablehnt, will nicht bei jedem
/// Start erneut gefragt werden - und wer es sich anders ueberlegt, findet den
/// Weg in den Einstellungen.
/// </remarks>
public partial class InstallPromptWindow : Window
{
    public InstallPromptWindow()
    {
        InitializeComponent();

        TargetText.Text = SelfInstaller.TargetPath;

        InstallButton.Click += (_, _) => Install();
        LaterButton.Click += (_, _) =>
        {
            Declined?.Invoke(this, EventArgs.Empty);
            Close();
        };
    }

    /// <summary>Die Einrichtung ist geglueckt; die neue Instanz laeuft bereits.</summary>
    public event EventHandler? Installed;

    /// <summary>Der Nutzer moechte nicht einrichten.</summary>
    public event EventHandler? Declined;

    private void Install()
    {
        InstallButton.IsEnabled = false;
        StatusText.Text = "Wird eingerichtet ...";
        StatusText.IsVisible = true;

        var ergebnis = SelfInstaller.Install();

        if (!ergebnis.Succeeded)
        {
            StatusText.Text = ergebnis.Message;
            InstallButton.IsEnabled = true;
            return;
        }

        StatusText.Text = ergebnis.Message;
        Installed?.Invoke(this, EventArgs.Empty);
    }
}
