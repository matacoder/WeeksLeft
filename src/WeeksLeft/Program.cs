namespace WeeksLeft;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var flags = new HashSet<string>(args.Select(a => a.ToLowerInvariant()));

        if (flags.Contains("--shoot"))
            return Shoot.Run(args.SkipWhile(a => !a.Equals("--shoot", StringComparison.OrdinalIgnoreCase))
                                 .Skip(1).ToArray());

        if (flags.Contains("--apply") || flags.Contains("--dry-run"))
            return ApplyRunner.Run(
                force: flags.Contains("--force") || flags.Contains("--dry-run"),
                dryRun: flags.Contains("--dry-run"));

        if (flags.Contains("--install-task"))
            return TaskSchedulerSetup.Install(AppConfig.Load()).ok ? 0 : 1;

        if (flags.Contains("--uninstall-task"))
            return TaskSchedulerSetup.Uninstall().ok ? 0 : 1;

        if (flags.Contains("--install"))
            return Installer.Install(AppConfig.Load()).ok ? 0 : 1;

        if (flags.Contains("--uninstall"))
            return Uninstall(quiet: flags.Contains("--quiet"));

        // Settings window — single instance.
        using var mutex = new Mutex(true, "WeeksLeft.Settings.SingleInstance", out bool isFirst);
        if (!isFirst) return 0;

        Application.Run(new SettingsForm());
        return 0;
    }

    /// <summary>Entry point used by Windows "Apps &amp; features".</summary>
    private static int Uninstall(bool quiet)
    {
        bool ru = System.Globalization.CultureInfo.CurrentUICulture
                    .TwoLetterISOLanguageName == "ru";
        bool removeSettings = false;

        if (!quiet)
        {
            var answer = MessageBox.Show(
                ru ? "Удалить WeeksLeft?\n\nОбои, которые сейчас стоят, останутся на месте."
                   : "Remove WeeksLeft?\n\nThe wallpaper currently on screen stays where it is.",
                "WeeksLeft", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (answer != DialogResult.OK) return 1602; // ERROR_INSTALL_USEREXIT

            removeSettings = MessageBox.Show(
                ru ? "Удалить также настройки и созданные обои?"
                   : "Also delete settings and generated wallpapers?",
                "WeeksLeft", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        return Installer.Uninstall(removeSettings).ok ? 0 : 1;
    }
}
