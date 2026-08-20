using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeeksLeft;

public sealed class Milestone
{
    public string Date { get; set; } = "";   // yyyy-MM-dd
    public string Label { get; set; } = "";
    public string? Color { get; set; }
}

public sealed class AppConfig
{
    // --- who ---
    public string? BirthDate { get; set; }                 // yyyy-MM-dd, null = not configured
    public string Sex { get; set; } = "male";              // male | female | average
    public string? Country { get; set; }                   // null/"auto" = detect from Windows home location

    // --- how long ---
    public string Model { get; set; } = "remaining";       // remaining | birth | custom
    public double CustomTargetAge { get; set; } = 90;
    public double? OverrideExpectancy { get; set; }        // hard override of total years, wins over Model

    // --- look ---
    public string Template { get; set; } = "01-grid";
    public string Rotation { get; set; } = "weekly";       // fixed | weekly | monthly
    // Picking a design by hand overrides the rotation, but only until the next period,
    // so "Apply now" always puts up exactly what the preview showed.
    public string? PinnedTemplate { get; set; }
    public long PinnedPeriod { get; set; } = -1;
    public string Tone { get; set; } = "positive";         // positive | neutral
    public string Theme { get; set; } = "auto";            // auto (follow Windows) | dark | light
    public string Accent { get; set; } = "system";         // "system" (Windows accent) or #RRGGBB
    public string Granularity { get; set; } = "weeks";     // weeks | months | days | years
    public string Lang { get; set; } = "auto";             // auto | ru | en

    public bool ShowNumbers { get; set; } = true;
    public bool ShowEndDate { get; set; } = false;         // off by default on purpose
    public string CustomText { get; set; } = "";

    // Safe zones as a share of the screen, so they are resolution independent.
    public double SafeLeftPercent { get; set; } = 0;       // desktop icon column
    public double SafeBottomPercent { get; set; } = 0;     // taskbar

    public List<Milestone> Milestones { get; set; } = new();

    // --- behaviour ---
    public string MonitorMode { get; set; } = "all-same";  // all-same | primary-only | per-monitor
    public string UpdateMode { get; set; } = "weekly";     // weekly | daily | logon
    public bool AutoStart { get; set; } = true;
    public bool KeepHistory { get; set; } = false;

    // --- state (written by --apply) ---
    public string? LastAppliedKey { get; set; }

    // ---------------------------------------------------------------

    [JsonIgnore]
    public static string Dir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeeksLeft");

    [JsonIgnore]
    public static string Path_ { get; } = Path.Combine(Dir, "config.json");

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(Path_))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Path_), Opts) ?? new AppConfig();
        }
        catch { /* corrupt config -> defaults */ }
        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        var tmp = Path_ + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Opts));
        File.Move(tmp, Path_, overwrite: true);
    }

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BirthDate);

    [JsonIgnore]
    public Sex SexEnum => Sex?.ToLowerInvariant() switch
    {
        "female" => WeeksLeft.Sex.Female,
        "average" => WeeksLeft.Sex.Average,
        _ => WeeksLeft.Sex.Male
    };

    [JsonIgnore]
    public string ResolvedCountry =>
        string.IsNullOrWhiteSpace(Country) || Country.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? Geo.DetectCountry()
            : Country;

    [JsonIgnore]
    public string ResolvedLang =>
        Lang is "ru" or "en" ? Lang
        : System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
}
