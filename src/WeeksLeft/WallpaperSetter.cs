using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WeeksLeft;

public sealed record MonitorInfo(string Id, int Width, int Height, bool IsPrimary)
{
    public string Signature => $"{Width}x{Height}";
}

public static class WallpaperSetter
{
    // ---------------- COM: IDesktopWallpaper (Windows 8+) ----------------

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private enum WallpaperPosition { Center = 0, Tile = 1, Stretch = 2, Fit = 3, Fill = 4, Span = 5 }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId,
                          [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        uint GetMonitorDevicePathCount();
        RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(WallpaperPosition position);
        WallpaperPosition GetPosition();
    }

    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    private class DesktopWallpaperClass { }

    // ---------------- Fallback: SystemParametersInfo ----------------

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    private const uint SPI_SETDESKWALLPAPER = 0x0014;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    // ---------------------------------------------------------------

    /// <summary>Physical resolution of every active monitor, paired with its stable device path.</summary>
    public static List<MonitorInfo> GetMonitors()
    {
        var result = new List<MonitorInfo>();
        try
        {
            var dw = (IDesktopWallpaper)new DesktopWallpaperClass();
            uint count = dw.GetMonitorDevicePathCount();
            for (uint i = 0; i < count; i++)
            {
                string id;
                RECT r;
                try
                {
                    id = dw.GetMonitorDevicePathAt(i);
                    if (string.IsNullOrEmpty(id)) continue;
                    r = dw.GetMonitorRECT(id);   // throws for detached monitors
                }
                catch { continue; }

                int w = r.Right - r.Left, h = r.Bottom - r.Top;
                if (w <= 0 || h <= 0) continue;
                result.Add(new MonitorInfo(id, w, h, r.Left == 0 && r.Top == 0));
            }
            Marshal.FinalReleaseComObject(dw);
        }
        catch { /* fall through to WinForms */ }

        if (result.Count == 0)
        {
            foreach (var s in Screen.AllScreens)
                result.Add(new MonitorInfo("", s.Bounds.Width, s.Bounds.Height, s.Primary));
        }
        if (result.Count == 0)
            result.Add(new MonitorInfo("", 1920, 1080, true));

        if (!result.Any(m => m.IsPrimary))
            result[0] = result[0] with { IsPrimary = true };

        return result;
    }

    /// <summary>Sets one image per monitor id. A null/empty id means "all monitors".</summary>
    public static void Apply(IReadOnlyList<(MonitorInfo monitor, string path)> assignments)
    {
        PrepareRegistry();

        try
        {
            var dw = (IDesktopWallpaper)new DesktopWallpaperClass();
            dw.SetPosition(WallpaperPosition.Fill);
            foreach (var (m, path) in assignments)
                dw.SetWallpaper(string.IsNullOrEmpty(m.Id) ? null : m.Id, path);
            Marshal.FinalReleaseComObject(dw);
            return;
        }
        catch { /* Windows 7 or COM blocked -> fallback below */ }

        var primary = assignments.FirstOrDefault(a => a.monitor.IsPrimary);
        var chosen = primary.path ?? assignments[0].path;
        SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, chosen, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }

    /// <summary>
    /// Fill mode, no tiling, and — importantly — tells Windows to stop re-encoding the
    /// wallpaper to a low quality JPEG, which otherwise destroys 4K gradients.
    /// </summary>
    private static void PrepareRegistry()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
            if (k is null) return;
            k.SetValue("WallpaperStyle", "10", RegistryValueKind.String); // 10 = Fill
            k.SetValue("TileWallpaper", "0", RegistryValueKind.String);
            k.SetValue("JPEGImportQuality", 100, RegistryValueKind.DWord);
        }
        catch { /* not fatal */ }
    }
}
