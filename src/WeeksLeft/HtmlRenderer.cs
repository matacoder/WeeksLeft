using System.Drawing;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace WeeksLeft;

/// <summary>
/// Renders an HTML template to a PNG at an exact pixel size using an off-screen WebView2.
/// One instance is reused for every monitor: the browser starts once, renders N images, exits.
/// </summary>
public sealed class HtmlRenderer : IAsyncDisposable
{
    private Form _host = null!;
    private CoreWebView2Environment _env = null!;
    private CoreWebView2Controller _ctrl = null!;
    private CoreWebView2 _web = null!;
    private string? _injectedScriptId;
    private TaskCompletionSource<bool>? _ready;

    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task<HtmlRenderer> CreateAsync()
    {
        var r = new HtmlRenderer();

        // A real window is required, but we park it beyond the virtual desktop so nothing flashes.
        var vs = SystemInformation.VirtualScreen;
        r._host = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(vs.Right + 200, vs.Top),
            Size = new Size(800, 600),
            MinimumSize = new Size(1, 1),
            Text = "WeeksLeft renderer"
        };
        r._host.Show();

        var opts = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments =
                "--hide-scrollbars --disable-extensions --no-first-run " +
                "--disable-background-timer-throttling --disable-renderer-backgrounding " +
                "--force-color-profile=srgb --disable-features=Translate,BackForwardCache"
        };

        r._env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(AppConfig.Dir, "wv2"),
            options: opts);

        r._ctrl = await r._env.CreateCoreWebView2ControllerAsync(r._host.Handle);
        r._ctrl.ShouldDetectMonitorScaleChanges = false;
        r._ctrl.RasterizationScale = 1.0;   // templates are written in vw/vh, so 1 CSS px == 1 device px
        r._ctrl.DefaultBackgroundColor = Color.Black;
        r._ctrl.IsVisible = true;

        r._web = r._ctrl.CoreWebView2;
        var s = r._web.Settings;
        s.AreDefaultContextMenusEnabled = false;
        s.AreDevToolsEnabled = false;
        s.IsStatusBarEnabled = false;
        s.IsZoomControlEnabled = false;
        s.AreBrowserAcceleratorKeysEnabled = false;
        s.IsPasswordAutosaveEnabled = false;
        s.IsGeneralAutofillEnabled = false;

        r._web.WebMessageReceived += (_, e) =>
        {
            try { if (e.TryGetWebMessageAsString() == "ready") r._ready?.TrySetResult(true); }
            catch { /* non-string message */ }
        };

        return r;
    }

    public async Task<byte[]> RenderAsync(string templatePath, IDictionary<string, object?> data,
                                          int width, int height, int timeoutMs = 8000)
    {
        _host.Size = new Size(width, height);
        _ctrl.Bounds = new Rectangle(0, 0, width, height);

        if (_injectedScriptId is not null)
        {
            _web.RemoveScriptToExecuteOnDocumentCreated(_injectedScriptId);
            _injectedScriptId = null;
        }
        var payload = JsonSerializer.Serialize(data, Json);
        _injectedScriptId = await _web.AddScriptToExecuteOnDocumentCreatedAsync(
            $"window.WEEKS_DATA = {payload};");

        _ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var navDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnNav(object? _, CoreWebView2NavigationCompletedEventArgs e) => navDone.TrySetResult(e.IsSuccess);
        _web.NavigationCompleted += OnNav;
        try
        {
            // Accept either a plain path or a ready-made URL, and cache-bust so repeated
            // renders of the same page always re-run its script.
            var url = templatePath.Contains("://")
                ? templatePath
                : new Uri(Path.GetFullPath(templatePath)).AbsoluteUri;
            url += (url.Contains('?') ? "&" : "?") + "t=" + Environment.TickCount64;
            _web.Navigate(url);
            await Task.WhenAny(navDone.Task, Task.Delay(timeoutMs));
        }
        finally { _web.NavigationCompleted -= OnNav; }

        // The page signals when fonts are loaded and the first frame is painted.
        await Task.WhenAny(_ready.Task, Task.Delay(timeoutMs));
        await Task.Delay(120); // one extra compositor beat

        using var ms = new MemoryStream();
        await _web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
        return ms.ToArray();
    }

    public ValueTask DisposeAsync()
    {
        try { _ctrl?.Close(); } catch { }
        try { _host?.Close(); _host?.Dispose(); } catch { }
        return ValueTask.CompletedTask;
    }
}
