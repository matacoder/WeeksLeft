using System.Globalization;

namespace WeeksLeft;

/// <summary>Everything a template needs, computed once and handed to the page as JSON.</summary>
public static class LifeState
{
    public const double DaysPerYear = 365.2425;

    public static Dictionary<string, object?> Build(AppConfig cfg, DateTime now, int width, int height)
    {
        var birth = DateTime.ParseExact(cfg.BirthDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var country = LifeData.Get(cfg.ResolvedCountry);
        var lang = cfg.ResolvedLang;

        double daysLived = Math.Max(0, (now.Date - birth.Date).TotalDays);
        double ageYears = daysLived / DaysPerYear;
        double e0 = country.E0(cfg.SexEnum);

        double totalYears = cfg.OverrideExpectancy ?? cfg.Model switch
        {
            "birth" => e0,
            "custom" => cfg.CustomTargetAge,
            _ => LifeMath.ExpectedFinalAge(e0, ageYears)
        };
        totalYears = Math.Clamp(totalYears, 1, LifeMath.MaxAge);

        double totalDays = totalYears * DaysPerYear;
        double daysLeft = Math.Max(0, totalDays - daysLived);

        long weeksLived = (long)Math.Floor(daysLived / 7.0);
        long weeksTotal = (long)Math.Round(totalDays / 7.0);
        long weeksLeft = Math.Max(0, weeksTotal - weeksLived);
        double percent = weeksTotal > 0 ? Math.Clamp(weeksLived * 100.0 / weeksTotal, 0, 100) : 100;

        // Classic life-in-weeks grid: one row per year of age, 52 cells per row.
        int gridRows = Math.Max((int)Math.Ceiling(totalYears), (int)Math.Floor(ageYears) + 1);
        gridRows = Math.Clamp(gridRows, 1, LifeMath.MaxAge);
        var (curRow, curCol) = GridCell(birth, now);

        var endDate = birth.AddDays(totalDays);

        return new Dictionary<string, object?>
        {
            ["birthDate"] = birth.ToString("yyyy-MM-dd"),
            ["now"] = now.ToString("yyyy-MM-dd"),
            ["lang"] = lang,

            ["ageYears"] = Math.Round(ageYears, 2),
            ["daysLived"] = (long)daysLived,
            ["daysLeft"] = (long)Math.Round(daysLeft),
            ["monthsLived"] = (long)Math.Floor(ageYears * 12),
            ["monthsTotal"] = (long)Math.Round(totalYears * 12),
            ["weeksLived"] = weeksLived,
            ["weeksTotal"] = weeksTotal,
            ["weeksLeft"] = weeksLeft,
            ["yearsTotal"] = Math.Round(totalYears, 1),
            ["yearsLeft"] = Math.Round(Math.Max(0, totalYears - ageYears), 1),
            ["percent"] = Math.Round(percent, 2),

            ["gridRows"] = gridRows,
            ["gridCols"] = 52,
            ["curRow"] = curRow,
            ["curCol"] = curCol,

            ["endDate"] = endDate.ToString("yyyy-MM-dd"),
            ["endYear"] = endDate.Year,

            ["country"] = country.Iso2,
            ["countryName"] = lang == "ru" ? country.NameRu : country.NameEn,
            ["sex"] = cfg.Sex,
            ["model"] = cfg.OverrideExpectancy is null ? cfg.Model : "override",

            ["granularity"] = cfg.Granularity,
            ["tone"] = cfg.Tone,
            ["showNumbers"] = cfg.ShowNumbers,
            ["showEndDate"] = cfg.ShowEndDate,
            ["customText"] = cfg.CustomText ?? "",

            ["theme"] = cfg.Theme == "auto" ? (Geo.IsSystemDark() ? "dark" : "light") : cfg.Theme,
            ["accent"] = cfg.Accent.Equals("system", StringComparison.OrdinalIgnoreCase)
                            ? Geo.SystemAccent() : cfg.Accent,

            ["safeLeftPct"] = cfg.SafeLeftPercent,
            ["safeBottomPct"] = cfg.SafeBottomPercent,
            ["width"] = width,
            ["height"] = height,

            ["milestones"] = (cfg.Milestones ?? new()).Select(m => MilestoneDto(m, birth)).Where(d => d != null).ToList()
        };
    }

    private static Dictionary<string, object?>? MilestoneDto(Milestone m, DateTime birth)
    {
        if (!DateTime.TryParseExact(m.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d)) return null;
        if (d < birth) return null;
        var (row, col) = GridCell(birth, d);
        return new Dictionary<string, object?>
        {
            ["date"] = m.Date,
            ["label"] = m.Label,
            ["color"] = m.Color,
            ["row"] = row,
            ["col"] = col,
            ["week"] = (long)Math.Floor((d.Date - birth.Date).TotalDays / 7.0)
        };
    }

    /// <summary>Row = completed years of age, Col = week within that year of life (0..51).</summary>
    private static (int row, int col) GridCell(DateTime birth, DateTime at)
    {
        int row = at.Year - birth.Year;
        var anniversary = SafeAnniversary(birth, birth.Year + row);
        if (at.Date < anniversary.Date) { row--; anniversary = SafeAnniversary(birth, birth.Year + row); }
        if (row < 0) return (0, 0);
        int col = (int)Math.Floor((at.Date - anniversary.Date).TotalDays / 7.0);
        return (row, Math.Clamp(col, 0, 51));
    }

    /// <summary>Handles 29 February birthdays in non-leap years.</summary>
    private static DateTime SafeAnniversary(DateTime birth, int year)
    {
        int day = Math.Min(birth.Day, DateTime.DaysInMonth(year, birth.Month));
        return new DateTime(year, birth.Month, day);
    }

    /// <summary>Identifies "the current wallpaper" — if this is unchanged, --apply does nothing.</summary>
    public static string StateKey(AppConfig cfg, DateTime now, string monitorSignature, string template)
    {
        var birth = DateTime.ParseExact(cfg.BirthDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        long unit = cfg.Granularity switch
        {
            "days" => (long)(now.Date - birth.Date).TotalDays,
            "months" => (long)Math.Floor((now.Date - birth.Date).TotalDays / DaysPerYear * 12),
            "years" => (long)Math.Floor((now.Date - birth.Date).TotalDays / DaysPerYear),
            _ => (long)Math.Floor((now.Date - birth.Date).TotalDays / 7.0)
        };
        string theme = cfg.Theme == "auto" ? (Geo.IsSystemDark() ? "d" : "l") : cfg.Theme[..1];
        string accent = cfg.Accent.Equals("system", StringComparison.OrdinalIgnoreCase) ? Geo.SystemAccent() : cfg.Accent;
        return string.Join('_', unit, template, theme, accent, cfg.Model, cfg.ResolvedCountry,
                           cfg.Sex, cfg.Tone, cfg.Granularity,
                           cfg.ShowEndDate ? 1 : 0, cfg.ShowNumbers ? 1 : 0, monitorSignature);
    }
}
