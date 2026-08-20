namespace WeeksLeft;

/// <summary>
/// Developer utility: WeeksLeft.exe --shoot &lt;file.html&gt; &lt;width&gt; &lt;height&gt; &lt;out.png&gt;
/// Renders any local page through the same off-screen WebView2 used for wallpapers.
/// Handy for checking a new template or the settings layout without opening a window.
/// </summary>
public static class Shoot
{
    public static int Run(string[] positional)
    {
        if (positional.Length < 4) return 64;

        string html = ToUrl(positional[0]), outPath = positional[3];
        if (!int.TryParse(positional[1], out int w) || !int.TryParse(positional[2], out int h))
            return 64;

        int exit = 0;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        var ctx = new ApplicationContext();

        var kick = new System.Windows.Forms.Timer { Interval = 1 };
        kick.Tick += async (_, _) =>
        {
            kick.Stop(); kick.Dispose();
            try
            {
                await using var r = await HtmlRenderer.CreateAsync();
                var data = LoadPreviewData(w, h);
                var png = await r.RenderAsync(html, data, w, h);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
                await File.WriteAllBytesAsync(outPath, png);
            }
            catch (Exception ex) { ApplyRunner.Log("shoot failed: " + ex); exit = 1; }
            finally { ctx.ExitThread(); }
        };
        kick.Start();

        Application.Run(ctx);
        return exit;
    }

    /// <summary>Turns "page.html?demo=en" into a file:// URL with the query preserved.</summary>
    private static string ToUrl(string arg)
    {
        if (arg.Contains("://")) return arg;
        int q = arg.IndexOf('?');
        if (q < 0) return arg;
        return new Uri(Path.GetFullPath(arg[..q])).AbsoluteUri + arg[q..];
    }

    private static Dictionary<string, object?> LoadPreviewData(int w, int h)
    {
        var cfg = AppConfig.Load();
        if (cfg.IsConfigured) return LifeState.Build(cfg, DateTime.Now, w, h);
        return new Dictionary<string, object?>();
    }
}
