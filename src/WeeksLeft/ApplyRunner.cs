using System.Security.Cryptography;
using System.Text;

namespace WeeksLeft;

/// <summary>
/// The headless path (WeeksLeft.exe --apply). Started by Task Scheduler.
/// Exits in ~30 ms when the week, theme, template and monitor layout are unchanged.
/// </summary>
public static class ApplyRunner
{
    public static string OutDir => Path.Combine(AppConfig.Dir, "out");
    public static string LogPath => Path.Combine(AppConfig.Dir, "last-run.log");

    private static readonly StringBuilder _log = new();

    public static void Log(string msg)
    {
        _log.AppendLine($"{DateTime.Now:HH:mm:ss.fff}  {msg}");
    }

    public static void FlushLog()
    {
        try
        {
            Directory.CreateDirectory(AppConfig.Dir);
            File.WriteAllText(LogPath, _log.ToString());
        }
        catch { }
    }

    /// <summary>Runs the async pipeline on a WinForms message pump (WebView2 needs one).</summary>
    public static int Run(bool force, bool dryRun = false)
    {
        int exit = 0;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        var ctx = new ApplicationContext();

        var kick = new System.Windows.Forms.Timer { Interval = 1 };
        kick.Tick += async (_, _) =>
        {
            kick.Stop();
            kick.Dispose();
            try { exit = await ApplyAsync(force, dryRun); }
            catch (Exception ex) { Log("FAILED: " + ex); exit = 1; }
            finally { FlushLog(); ctx.ExitThread(); }
        };
        kick.Start();

        Application.Run(ctx);
        return exit;
    }

    /// <summary>When <paramref name="dryRun"/> is set the PNGs are written but the desktop is left alone.</summary>
    public static async Task<int> ApplyAsync(bool force, bool dryRun = false)
    {
        var cfg = AppConfig.Load();
        if (!cfg.IsConfigured) { Log("not configured, nothing to do"); return 2; }

        var now = DateTime.Now;
        var monitors = WallpaperSetter.GetMonitors();
        var targets = cfg.MonitorMode == "primary-only"
            ? monitors.Where(m => m.IsPrimary).ToList()
            : monitors;
        if (targets.Count == 0) targets = monitors;

        var allTemplates = Templates.All();
        var baseTemplate = Templates.Pick(cfg, now);

        // Which template each monitor uses.
        var perMonitor = new List<(MonitorInfo m, TemplateInfo t)>();
        for (int i = 0; i < targets.Count; i++)
        {
            var t = cfg.MonitorMode == "per-monitor" && allTemplates.Count > 0
                ? allTemplates[i % allTemplates.Count]
                : baseTemplate;
            perMonitor.Add((targets[i], t));
        }

        string signature = string.Join(",", perMonitor.Select(p => $"{p.m.Id}:{p.m.Signature}:{p.t.Id}"));
        string key = LifeState.StateKey(cfg, now, Hash(signature), baseTemplate.Id);

        if (!force && key == cfg.LastAppliedKey && CurrentFilesExist(cfg, key, perMonitor))
        {
            Log($"up to date (key {key}) — exiting without rendering");
            return 0;
        }

        Log($"rendering: key={key}, monitors={perMonitor.Count}");
        Directory.CreateDirectory(OutDir);

        // Render once per distinct (template, resolution) pair, reuse the file for identical monitors.
        var cache = new Dictionary<string, string>();
        var assignments = new List<(MonitorInfo, string)>();

        await using var renderer = await HtmlRenderer.CreateAsync();
        Log("webview2 ready");

        foreach (var (m, t) in perMonitor)
        {
            string cacheKey = $"{t.Id}|{m.Width}x{m.Height}";
            if (!cache.TryGetValue(cacheKey, out var path))
            {
                var data = LifeState.Build(cfg, now, m.Width, m.Height);
                var png = await renderer.RenderAsync(t.File, data, m.Width, m.Height);
                path = Path.Combine(OutDir, $"{t.Id}_{m.Width}x{m.Height}_{Hash(key + cacheKey)}.png");
                await File.WriteAllBytesAsync(path, png);
                cache[cacheKey] = path;
                Log($"rendered {t.Id} {m.Width}x{m.Height} -> {png.Length / 1024} KB");
            }
            assignments.Add((m, path));
        }

        if (dryRun)
        {
            Log("dry run — PNGs written, desktop untouched:");
            foreach (var p in cache.Values) Log("  " + p);
            return 0;
        }

        WallpaperSetter.Apply(assignments);
        Log("wallpaper applied");

        cfg.LastAppliedKey = key;
        cfg.Save();

        if (!cfg.KeepHistory) Cleanup(cache.Values.ToHashSet(StringComparer.OrdinalIgnoreCase));
        return 0;
    }

    private static bool CurrentFilesExist(AppConfig cfg, string key,
        List<(MonitorInfo m, TemplateInfo t)> perMonitor)
    {
        foreach (var (m, t) in perMonitor)
        {
            string cacheKey = $"{t.Id}|{m.Width}x{m.Height}";
            var path = Path.Combine(OutDir, $"{t.Id}_{m.Width}x{m.Height}_{Hash(key + cacheKey)}.png");
            if (!File.Exists(path)) return false;
        }
        return true;
    }

    private static void Cleanup(HashSet<string> keep)
    {
        try
        {
            foreach (var f in Directory.GetFiles(OutDir, "*.png"))
                if (!keep.Contains(f)) File.Delete(f);
        }
        catch { /* Windows may still hold the previous file */ }
    }

    private static string Hash(string s)
    {
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(b, 0, 5).ToLowerInvariant();
    }
}
