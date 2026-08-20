using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WeeksLeft;

/// <summary>
/// The settings window is itself an HTML page in WebView2 — the same engine that renders
/// the wallpaper — so the design gallery shows the real templates, live, not screenshots.
/// </summary>
public sealed class SettingsForm : Form
{
    private const string Host = "weeksleft.assets";   // NOT .local — that TLD triggers an mDNS lookup
    private readonly WebView2 _view = new() { Dock = DockStyle.Fill };
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private bool _busy;

    /// <summary>Startup timings, flushed once the window is up rather than on every mark.</summary>
    private void Mark(string what)
    {
        ApplyRunner.Log($"ui +{_clock.ElapsedMilliseconds,5} ms  {what}");
        if (what.Contains("first paint")) ApplyRunner.FlushLog();
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = null
    };

    public SettingsForm()
    {
        Text = "WeeksLeft";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 640);
        Size = new Size(1180, 820);
        BackColor = Color.FromArgb(14, 14, 17);
        Controls.Add(_view);
        Load += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        Mark("form load");
        var env = await CoreWebView2Environment.CreateAsync(
            null, Path.Combine(AppConfig.Dir, "wv2-ui"), null);
        Mark("environment created");

        await _view.EnsureCoreWebView2Async(env);
        Mark("webview ready");

        var core = _view.CoreWebView2;
        core.NavigationCompleted += (_, _) => Mark("navigation completed");
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;

        core.SetVirtualHostNameToFolderMapping(
            Host, Path.Combine(AppContext.BaseDirectory, "assets"),
            CoreWebView2HostResourceAccessKind.Allow);

        core.WebMessageReceived += OnMessage;
        core.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "assets", "settings.html")).AbsoluteUri);
    }

    private async void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        JsonNode? msg;
        try { msg = JsonNode.Parse(e.WebMessageAsJson); }
        catch { return; }
        if (msg is null) return;

        string cmd = msg["cmd"]?.GetValue<string>() ?? "";
        try
        {
            switch (cmd)
            {
                case "init":
                    Mark("init requested");
                    Post(new { type = "state", state = BuildState() });
                    Mark("state sent");
                    break;

                case "mark":
                    Mark("page: " + (msg["what"]?.GetValue<string>() ?? ""));
                    break;

                // Autosave: everything typed survives closing the window, no button needed.
                case "persist":
                    {
                        var cfg = ConfigFrom(msg["config"]);
                        cfg.Save();
                        break;
                    }

                case "preview":
                    Post(new { type = "preview", data = BuildPreview(ConfigFrom(msg["config"])) });
                    break;

                case "save":
                    {
                        var cfg = ConfigFrom(msg["config"]);
                        PinChosenDesign(cfg);
                        cfg.LastAppliedKey = null;      // force a repaint next apply
                        cfg.Save();
                        SyncAutoStart(cfg);
                        Post(new { type = "saved" });
                        break;
                    }

                case "apply":
                    {
                        if (_busy) return;
                        _busy = true;
                        var cfg = ConfigFrom(msg["config"]);
                        PinChosenDesign(cfg);
                        cfg.LastAppliedKey = null;
                        cfg.Save();
                        SyncAutoStart(cfg);
                        int code;
                        try { code = await ApplyRunner.ApplyAsync(force: true); }
                        catch (Exception ex) { ApplyRunner.Log("FAILED: " + ex); code = 1; }
                        finally { _busy = false; }
                        Post(new { type = "applied", ok = code == 0, code });
                        break;
                    }

                case "install":
                    {
                        var cfg = ConfigFrom(msg["config"]);
                        cfg.AutoStart = true;
                        cfg.Save();
                        var (ok, message) = Installer.Install(cfg);
                        Post(new { type = "installed", ok, message, state = BuildState() });
                        break;
                    }

                case "uninstall":
                    {
                        var (ok, message) = Installer.Uninstall(removeSettings: false);
                        Post(new { type = "uninstalled", ok, message, state = BuildState() });
                        break;
                    }

                case "openFolder":
                    TryOpen(ApplyRunner.OutDir);
                    break;

                case "openLog":
                    TryOpen(ApplyRunner.LogPath);
                    break;

                case "close":
                    Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            Post(new { type = "error", message = ex.Message });
        }
    }

    private void Post(object payload) =>
        _view.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload, Json));

    private static void TryOpen(string path)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
            { UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// The design showing in the preview is the design the user means. With rotation on,
    /// pin it for the rest of this week (or month) so "Apply now" is not overruled by the
    /// rotation the moment it runs; the cycle picks up again next period.
    /// </summary>
    private static void PinChosenDesign(AppConfig cfg)
    {
        if (cfg.Rotation is not ("weekly" or "monthly")) return;
        if (string.IsNullOrEmpty(cfg.Template)) return;

        cfg.PinnedTemplate = cfg.Template;
        cfg.PinnedPeriod = Templates.RotationIndex(cfg, DateTime.Now);
    }

    private static void SyncAutoStart(AppConfig cfg)
    {
        if (cfg.AutoStart)
            TaskSchedulerSetup.Install(cfg, Installer.IsInstalled ? Installer.InstalledExe : null);
        else if (TaskSchedulerSetup.IsInstalled())
            TaskSchedulerSetup.Uninstall();
    }

    private static AppConfig ConfigFrom(JsonNode? node)
    {
        if (node is null) return AppConfig.Load();
        var cfg = JsonSerializer.Deserialize<AppConfig>(node.ToJsonString(), Json) ?? new AppConfig();
        return cfg;
    }

    private object BuildState()
    {
        var cfg = AppConfig.Load();
        var monitors = WallpaperSetter.GetMonitors();
        return new
        {
            config = cfg,
            detectedCountry = Geo.DetectCountry(),
            systemAccent = Geo.SystemAccent(),
            systemDark = Geo.IsSystemDark(),
            lang = cfg.ResolvedLang,
            taskInstalled = TaskSchedulerSetup.IsInstalled(),
            installed = Installer.IsInstalled,
            runningFromInstall = Installer.RunningFromInstall,
            installDir = Installer.InstallDir,
            countries = LifeData.All.Select(c => new { c.Iso2, c.NameRu, c.NameEn, c.E0Male, c.E0Female }),
            templates = Templates.All().Select(t => new { t.Id, t.NameRu, t.NameEn }),
            currentTemplate = SafeCurrentTemplate(cfg),
            monitors = monitors.Select(m => new { m.Width, m.Height, m.IsPrimary })
        };
    }

    /// <summary>The design that is actually on the desktop right now.</summary>
    private static string SafeCurrentTemplate(AppConfig cfg)
    {
        try { return Templates.Pick(cfg, DateTime.Now).Id; }
        catch { return cfg.Template; }
    }

    private static object? BuildPreview(AppConfig cfg)
    {
        if (!cfg.IsConfigured) return null;
        try
        {
            // 1920 wide at the primary monitor's aspect ratio — the page is scaled down in the UI,
            // so the preview is a true reduction of the real wallpaper.
            var m = WallpaperSetter.GetMonitors().FirstOrDefault(x => x.IsPrimary);
            int h = m is null ? 1080 : (int)Math.Round(1920.0 * m.Height / m.Width);
            return LifeState.Build(cfg, DateTime.Now, 1920, h);
        }
        catch { return null; }
    }
}
