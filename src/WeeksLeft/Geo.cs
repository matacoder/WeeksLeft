using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WeeksLeft;

/// <summary>Country + theme + accent colour, all read from Windows. No network, no IP lookup.</summary>
public static class Geo
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetUserDefaultGeoName(char[] geoName, int geoNameCount);

    /// <summary>ISO 3166-1 alpha-2 from the user's Windows "Home location" setting.</summary>
    public static string DetectCountry()
    {
        try
        {
            var buf = new char[16];
            int n = GetUserDefaultGeoName(buf, buf.Length);
            if (n > 1)
            {
                var s = new string(buf, 0, n - 1).Trim();
                if (s.Length == 2) return s.ToUpperInvariant();
            }
        }
        catch { /* pre-1709 Windows */ }

        try { return RegionInfo.CurrentRegion.TwoLetterISORegionName.ToUpperInvariant(); }
        catch { return "ZZ"; }
    }

    /// <summary>True when Windows apps are set to the dark theme.</summary>
    public static bool IsSystemDark()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return (k?.GetValue("AppsUseLightTheme") as int?) == 0;
        }
        catch { return true; }
    }

    /// <summary>Windows accent colour as #RRGGBB.</summary>
    public static string SystemAccent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (k?.GetValue("ColorizationColor") is int argb)
            {
                uint v = unchecked((uint)argb);
                return $"#{(v >> 16) & 0xFF:X2}{(v >> 8) & 0xFF:X2}{v & 0xFF:X2}";
            }
        }
        catch { /* ignore */ }
        return "#E8552D";
    }
}
