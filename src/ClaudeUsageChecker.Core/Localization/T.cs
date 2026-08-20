namespace ClaudeUsageChecker.Core.Localization;

/// <summary>
/// The interface texts, named instead of scattered as string literals.
/// </summary>
/// <remarks>
/// <para>
/// The short name is deliberate: this class appears in a great many places, and
/// <c>T.Exit</c> interrupts the reading less than a long identifier would.
/// </para>
/// <para>
/// Every member fetches its text from the <see cref="Localizer"/> currently in
/// force - which is why a language change takes effect as soon as the windows
/// refresh their labels. If a key is missing, the key itself comes back;
/// <c>LanguageFileTests</c> catches exactly that by checking every member here
/// against every language file.
/// </para>
/// </remarks>
public static class T
{
    private static Localizer L => Localizer.Current;

    // General
    public static string AppName => L["app.name"];
    public static string Close => L["app.close"];
    public static string Cancel => L["app.cancel"];
    public static string Save => L["app.save"];
    public static string Refresh => L["app.refresh"];
    public static string Unknown => L["app.unknown"];
    public static string Version(string version) => L.Format("app.version", version);

    /// <summary>The same, but saying plainly that this is a test build.</summary>
    public static string VersionPreRelease(string version) =>
        L.Format("app.versionPreRelease", version);

    // Tray
    public static string TrayShowDetails => L["tray.showDetails"];
    public static string TrayRefreshNow => L["tray.refreshNow"];
    public static string TraySettings => L["tray.settings"];
    public static string TrayCheckForUpdates => L["tray.checkUpdates"];
    public static string TrayAbout(string version) => L.Format("tray.about", version);
    public static string TrayExit => L["tray.exit"];
    public static string TrayNoData => L["tray.noData"];
    public static string TrayNoLimits => L["tray.noLimits"];

    // Tooltip
    public static string TooltipLoading => L["tooltip.loading"];
    public static string TooltipNotSignedIn => L["tooltip.notSignedIn"];
    public static string TooltipTokenExpired => L["tooltip.tokenExpired"];
    public static string TooltipOffline => L["tooltip.offline"];
    public static string TooltipNoData => L["tooltip.noData"];
    public static string TooltipNoLimits => L["tooltip.noLimits"];
    public static string TooltipStale => L["tooltip.stale"];
    public static string TooltipSession => L["tooltip.session"];
    public static string TooltipWeekly => L["tooltip.weekly"];

    public static string TooltipLine(string label, double percent, string moment, string remaining) =>
        L.Format("tooltip.line", label, percent, moment, remaining);

    // Sentences of their own for a window whose reset is already due. The
    // building block for a duration does not fit a slot that expects one - "in
    // now" is no sentence in any of the nine languages.
    public static string TooltipLineDue(string label, double percent) =>
        L.Format("tooltip.lineDue", label, percent);

    // Usage windows
    public static string WindowSession => L["usage.session"];
    public static string WindowWeeklyAll => L["usage.weeklyAll"];
    public static string WindowWeeklyModel(string model) => L.Format("usage.weeklyModel", model);
    public static string MenuLine(string label, double percent, string remaining) =>
        L.Format("usage.menuLine", label, percent, remaining);
    public static string DetailLine(string label, double percent, string remaining, string moment) =>
        L.Format("usage.detailLine", label, percent, remaining, moment);
    public static string MenuLineDue(string label, double percent) =>
        L.Format("usage.menuLineDue", label, percent);
    public static string DetailLineDue(string label, double percent, string moment) =>
        L.Format("usage.detailLineDue", label, percent, moment);
    public static string NotAvailable(string label) => L.Format("usage.notAvailable", label);
    public static string Percent(double value) => L.Format("usage.percent", value);
    public static string ResetIn(string remaining, string moment) => L.Format("usage.resetIn", remaining, moment);
    public static string ResetDue(string moment) => L.Format("usage.resetDue", moment);

    // Extra usage
    public static string ExtraTitle => L["extra.title"];
    public static string ExtraActive => L["extra.active"];
    public static string ExtraLine(string content) => L.Format("extra.line", content);
    public static string ExtraLineActive => L["extra.lineActive"];
    // The amounts arrive already written out, with the currency of the account -
    // formatting them here would mean deciding on a currency the API named.
    public static string ExtraUsedOfLimit(string used, string limit) =>
        L.Format("extra.usedOfLimit", used, limit);
    public static string ExtraUsedOfLimitLong(string used, string limit) =>
        L.Format("extra.usedOfLimitLong", used, limit);
    public static string ExtraUsedOnly(string used) => L.Format("extra.usedOnly", used);
    public static string ExtraMonthlyLimit(string limit) => L.Format("extra.limitOnly", limit);

    // Durations
    public static string DurationNow => L["duration.now"];
    public static string DurationDaysHours(int days, int hours) => L.Format("duration.daysHours", days, hours);
    public static string DurationDays(int days) => L.Format("duration.days", days);
    public static string DurationHoursMinutes(int hours, int minutes) =>
        L.Format("duration.hoursMinutes", hours, minutes);
    public static string DurationHours(int hours) => L.Format("duration.hours", hours);
    public static string DurationMinutes(int minutes) => L.Format("duration.minutes", minutes);

    // Details window
    public static string DetailsFooter(DateTimeOffset retrievedAt, string source) =>
        L.Format("details.footer", retrievedAt, source);
    public static string DetailsNoDataYet => L["details.noDataYet"];
    public static string DetailsInstall => L["details.install"];
    public static string DetailsReleasePage => L["details.releasePage"];
    public static string DetailsStale(string reason) => L.Format("details.stale", reason);
    public static string DetailsNotConfigured => L["details.notConfigured"];
    public static string DetailsSignInExpired(string reason) => L.Format("details.signInExpired", reason);

    // Token sources
    public static string SourceOAuth => L["source.oauth"];
    public static string SourceSecretStore => L["source.secretStore"];
    public static string SourceEnvironment => L["source.environment"];
    public static string SourceClaudeCli => L["source.claudeCli"];

    // Settings
    public static string SettingsTitle => L["settings.title"];
    public static string SettingsAccountsSection => L["settings.accountsSection"];
    public static string SettingsAccountOwn => L["settings.accountOwn"];
    public static string SettingsAccountChecking => L["settings.accountChecking"];
    public static string SettingsAccountSignedIn => L["settings.accountSignedIn"];
    public static string SettingsAccountSignedInUntil(DateTimeOffset expiry) =>
        L.Format("settings.accountSignedInUntil", expiry);
    public static string SettingsAccountNotSignedIn => L["settings.accountNotSignedIn"];
    public static string SettingsAccountExpired => L["settings.accountExpired"];
    public static string SettingsAccountNoStore => L["settings.accountNoStore"];
    public static string SettingsSignInSection => L["settings.signInSection"];
    public static string SettingsSignIn => L["settings.signInButton"];
    public static string SettingsSignOut => L["settings.signOutButton"];
    public static string SettingsNotSignedIn => L["settings.signedOut"];
    public static string SettingsSignedIn(string scope, DateTimeOffset validUntil) =>
        L.Format("settings.signedIn", scope, validUntil);
    public static string NoSecureStore => L["settings.noSecureStore"];
    public static string SettingsBehaviourSection => L["settings.behaviourSection"];
    public static string SettingsInterval => L["settings.interval"];
    public static string SettingsLaunchAtLogin => L["settings.launchAtLogin"];
    public static string SettingsLaunchAtLoginMac => L["settings.launchAtLoginMac"];
    public static string SettingsCheckForUpdates => L["settings.checkUpdates"];
    public static string SettingsRefreshChecksForUpdates => L["settings.refreshChecksUpdates"];
    public static string SettingsLanguageSection => L["settings.languageSection"];
    public static string SettingsLanguageHint => L["settings.languageHint"];
    public static string SettingsLanguageLabel => L["settings.languageLabel"];
    public static string SettingsChannelSection => L["settings.channelSection"];
    public static string SettingsChannelHint => L["settings.channelHint"];
    public static string SettingsChannelLabel => L["settings.channelLabel"];
    public static string SettingsChannelStable => L["settings.channelStable"];
    public static string SettingsChannelPreRelease => L["settings.channelPreRelease"];
    public static string SettingsThemeSection => L["settings.themeSection"];
    public static string SettingsThemeHint => L["settings.themeHint"];
    public static string SettingsThemeLabel => L["settings.themeLabel"];
    public static string SettingsThemeSystem => L["settings.themeSystem"];
    public static string SettingsThemeLight => L["settings.themeLight"];
    public static string SettingsThemeDark => L["settings.themeDark"];
    public static string SettingsThresholdSection => L["settings.thresholdSection"];
    public static string SettingsThresholdHint => L["settings.thresholdHint"];
    public static string SettingsWarningThreshold => L["settings.warningThreshold"];
    public static string SettingsCriticalThreshold => L["settings.criticalThreshold"];
    public static string ThresholdTooSmall(double minimum) => L.Format("settings.thresholdMinimum", minimum);
    public static string ThresholdTooLarge => L["settings.thresholdMaximum"];
    public static string ThresholdOrder => L["settings.thresholdOrder"];
    public static string SettingsRelocationHint(string target) => L.Format("settings.relocationHint", target);
    public static string SettingsRelocating => L["settings.relocating"];

    // Sign-in
    public static string SignInTitle => L["signin.title"];
    public static string SignInHeading => L["signin.heading"];
    public static string SignInIntro => L["signin.intro"];
    public static string SignInStep1 => L["signin.step1"];
    public static string SignInOpenBrowser => L["signin.openBrowser"];
    public static string SignInUrlHint => L["signin.urlHint"];
    public static string SignInStep2 => L["signin.step2"];
    public static string SignInCodeWatermark => L["signin.codeWatermark"];
    public static string SignInComplete => L["signin.complete"];
    public static string SignInNotYet => L["signin.notSignedIn"];
    public static string SignInSignedIn(DateTimeOffset validUntil, string scope) =>
        L.Format("signin.signedIn", validUntil, scope);
    public static string SignInUnavailable => L["signin.unavailable"];
    public static string SignInGrantInBrowser => L["signin.grantInBrowser"];
    public static string SignInBrowserFailed => L["signin.browserFailed"];
    public static string SignInOpenPageFirst => L["signin.openPageFirst"];
    public static string SignInPasteCode => L["signin.pasteCode"];
    public static string SignInRedeeming => L["signin.redeeming"];
    public static string SignInSuccess => L["signin.success"];
    public static string SignInSaveFailed(string reason) => L.Format("signin.saveFailed", reason);

    // Setup prompt
    public static string InstallTitle => L["install.title"];
    public static string InstallHeading => L["install.heading"];
    public static string InstallIntro => L["install.intro"];
    public static string InstallListIntro => L["install.listIntro"];
    public static string InstallBulletCopy => L["install.bulletCopy"];
    public static string InstallBulletAutostart => L["install.bulletAutostart"];
    public static string InstallBulletRestart => L["install.bulletRestart"];
    public static string InstallOutro => L["install.outro"];
    public static string InstallLanguage => L["install.languageLabel"];
    public static string InstallLanguageHint => L["install.languageHint"];
    public static string InstallAccept => L["install.accept"];
    public static string InstallDecline => L["install.decline"];
    public static string InstallRunning => L["install.running"];

    // About
    public static string AboutTitle => L["about.title"];
    public static string AboutDescription => L["about.description"];
    public static string AboutRepository => L["about.repository"];
    public static string AboutReleaseNotes => L["about.releaseNotes"];
    public static string AboutLicense(string copyright) => L.Format("about.license", copyright);

    // Release notes
    public static string NotesTitle => L["notes.title"];
    public static string NotesHeading(string version) => L.Format("notes.headingSingle", version);
    public static string NotesHeadingMultiple(string version, int others) =>
        L.Format("notes.headingMultiple", version, others);
    public static string NotesPrevious(string version) => L.Format("notes.previous", version);
    public static string NotesPreRelease(string version) => L.Format("notes.preRelease", version);
    public static string NotesNone => L["notes.none"];
    public static string NotesNoneHint => L["notes.noneHint"];
    public static string NotesRelease(string version) => L.Format("notes.releaseHeading", version);
    public static string NotesReleaseDated(string version, string date) =>
        L.Format("notes.releaseHeadingDated", version, date);
    public static string NotesTranslationMissing => L["notes.translationMissing"];

    // Updates
    public static string UpdateUpToDate(string version) => L.Format("update.upToDate", version);
    public static string UpdateUpToDatePreRelease(string version) =>
        L.Format("update.upToDatePreRelease", version);
    public static string UpdateAvailable(string available, string installed) =>
        L.Format("update.available", available, installed);
    public static string UpdateAvailablePreRelease(string available, string installed) =>
        L.Format("update.availablePreRelease", available, installed);
    public static string UpdateNoRelease => L["update.noRelease"];
    public static string UpdateHttpError(int status) => L.Format("update.httpError", status);
    public static string UpdateIncomplete => L["update.incomplete"];
    public static string UpdateUnknownFormat(string tag) => L.Format("update.unknownFormat", tag);
    public static string UpdateCheckFailed => L["update.checkFailed"];
    public static string UpdateDownloading => L["update.downloading"];

    // Self-installation
    public static string InstallerLocationUnknown => L["installer.locationUnknown"];
    public static string InstallerAlreadyInPlace => L["installer.alreadyInPlace"];
    public static string InstallerCopyFailed(string reason) => L.Format("installer.copyFailed", reason);
    public static string InstallerStartFailed(string reason) => L.Format("installer.startFailed", reason);
    public static string InstallerDone => L["installer.done"];

    // Self-update
    public static string UpdaterNotSelfReplaceable => L["updater.notSelfReplaceable"];
    public static string UpdaterMissingFileOrChecksum => L["updater.missingFileOrChecksum"];
    public static string UpdaterChecksumUnreadable => L["updater.checksumUnreadable"];
    public static string UpdaterChecksumMismatch => L["updater.checksumMismatch"];
    public static string UpdaterDownloadFailed(string reason) => L.Format("updater.downloadFailed", reason);
    public static string UpdaterSaveFailed(string reason) => L.Format("updater.saveFailed", reason);
    public static string UpdaterReplaceFailed(string reason) => L.Format("updater.replaceFailed", reason);
    public static string UpdaterDone => L["updater.done"];

    // Errors
    public static string ErrorNoToken => L["error.noToken"];
    public static string ErrorNotSignedIn => L["error.notSignedIn"];
    public static string ErrorTimeout => L["error.timeout"];
    public static string ErrorUnreachable => L["error.unreachable"];
    public static string ErrorUnreadable => L["error.unreadable"];
    public static string ErrorEmptyResponse => L["error.emptyResponse"];
    public static string ErrorRateLimited => L["error.rateLimited"];
    public static string ErrorServer(int status) => L.Format("error.serverError", status);
    public static string ErrorUnexpectedResponse(int status) => L.Format("error.unexpected", status);
    public static string ErrorUnexpectedFetch => L["error.unexpectedFetch"];
    public static string ErrorMissingScope => L["error.missingScope"];
    public static string ErrorTokenRejected => L["error.tokenRejected"];
    public static string ErrorNoSecureStore => L["error.noSecureStore"];
    public static string SecretReadFailed(string key, int error) =>
        L.Format("secret.readFailed", key, error);
    public static string SecretWriteFailed(string key, int error) =>
        L.Format("secret.writeFailed", key, error);
    public static string SecretDeleteFailed(string key, int error) =>
        L.Format("secret.deleteFailed", key, error);

    // OAuth flow
    public static string OAuthWrongFlow => L["oauth.wrongFlow"];
    public static string OAuthRedeemFailed => L["oauth.redeemFailed"];
    public static string OAuthRefreshFailed => L["oauth.refreshFailed"];
    public static string OAuthUnreachable(string operation) => L.Format("oauth.unreachable", operation);
    public static string OAuthUnreadable(string operation) => L.Format("oauth.unreadable", operation);
    public static string OAuthNoToken(string operation) => L.Format("oauth.noToken", operation);
}
