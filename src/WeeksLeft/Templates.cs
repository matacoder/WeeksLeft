using System.Text.Json;

namespace WeeksLeft;

public sealed record TemplateInfo(string Id, string File, string NameRu, string NameEn);

public static class Templates
{
    public static string Dir => Path.Combine(AppContext.BaseDirectory, "assets", "templates");

    public static IReadOnlyList<TemplateInfo> All()
    {
        var names = new Dictionary<string, (string ru, string en)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var manifest = Path.Combine(Dir, "manifest.json");
            if (File.Exists(manifest))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
                foreach (var e in doc.RootElement.EnumerateArray())
                    names[e.GetProperty("id").GetString()!] =
                        (e.GetProperty("ru").GetString()!, e.GetProperty("en").GetString()!);
            }
        }
        catch { /* manifest is optional */ }

        var list = new List<TemplateInfo>();
        if (!Directory.Exists(Dir)) return list;

        foreach (var f in Directory.GetFiles(Dir, "*.html").OrderBy(x => x, StringComparer.Ordinal))
        {
            var id = Path.GetFileNameWithoutExtension(f);
            var (ru, en) = names.TryGetValue(id, out var n) ? n : (id, id);
            list.Add(new TemplateInfo(id, f, ru, en));
        }
        return list;
    }

    public static TemplateInfo? Find(string id) =>
        All().FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Which rotation period we are in — the week or month number since birth.</summary>
    public static long RotationIndex(AppConfig cfg, DateTime now)
    {
        if (!cfg.IsConfigured) return 0;
        var birth = DateTime.ParseExact(cfg.BirthDate!, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture);
        double days = (now.Date - birth.Date).TotalDays;
        return cfg.Rotation == "monthly"
            ? (long)Math.Floor(days / LifeState.DaysPerYear * 12)
            : (long)Math.Floor(days / 7.0);
    }

    /// <summary>Applies the rotation setting to pick which template this period uses.</summary>
    public static TemplateInfo Pick(AppConfig cfg, DateTime now)
    {
        var all = All();
        if (all.Count == 0) throw new FileNotFoundException($"No templates found in {Dir}");

        if (cfg.Rotation is not ("weekly" or "monthly") || !cfg.IsConfigured)
            return Find(cfg.Template) ?? all[0];

        long idx = RotationIndex(cfg, now);

        // A design picked by hand wins for the rest of this period, then rotation resumes.
        if (cfg.PinnedPeriod == idx && !string.IsNullOrEmpty(cfg.PinnedTemplate))
        {
            var pinned = Find(cfg.PinnedTemplate);
            if (pinned is not null) return pinned;
        }

        return all[(int)(((idx % all.Count) + all.Count) % all.Count)];
    }
}
