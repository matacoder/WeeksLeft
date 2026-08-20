using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace WeeksLeft;

/// <summary>
/// Per-user install: copies the app under %LOCALAPPDATA%\Programs\WeeksLeft, adds a Start
/// menu shortcut, registers the scheduled task against the installed copy, and shows up in
/// Windows "Apps &amp; features". No administrator rights, nothing written outside the user profile.
/// </summary>
public static class Installer
{
    public const string ProductName = "WeeksLeft";
    private const string UninstallKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WeeksLeft";

    public static string InstallDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs", ProductName);

    public static string InstalledExe => Path.Combine(InstallDir, "WeeksLeft.exe");

    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        "Programs", ProductName + ".lnk");

    /// <summary>True when the running process is the installed copy.</summary>
    public static bool IsInstalled =>
        File.Exists(InstalledExe);

    public static bool RunningFromInstall =>
        string.Equals(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
                      Path.TrimEndingDirectorySeparator(InstallDir),
                      StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ install

    public static (bool ok, string message) Install(AppConfig cfg)
    {
        try
        {
            if (!RunningFromInstall)
                CopyTree(AppContext.BaseDirectory, InstallDir);

            CreateShortcut();
            RegisterUninstallEntry();

            var (taskOk, taskOut) = TaskSchedulerSetup.Install(cfg, InstalledExe);
            if (!taskOk) return (false, "Scheduled task failed: " + taskOut);

            return (true, InstallDir);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>Removes the task, shortcut and registry entry; optionally wipes settings.</summary>
    public static (bool ok, string message) Uninstall(bool removeSettings)
    {
        try
        {
            TaskSchedulerSetup.Uninstall();

            try { if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, throwOnMissingSubKey: false); } catch { }

            if (removeSettings)
            {
                try { if (Directory.Exists(AppConfig.Dir)) Directory.Delete(AppConfig.Dir, true); }
                catch { }
            }

            if (Directory.Exists(InstallDir))
            {
                if (RunningFromInstall) ScheduleSelfDelete(InstallDir);
                else { try { Directory.Delete(InstallDir, true); } catch { ScheduleSelfDelete(InstallDir); } }
            }

            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ------------------------------------------------------------------ pieces

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(from.TrimEnd('\\'), to.TrimEnd('\\')));

        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            // Documentation and debug leftovers do not belong in an install.
            var ext = Path.GetExtension(file);
            if (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".pdb", StringComparison.OrdinalIgnoreCase)) continue;

            var target = file.Replace(from.TrimEnd('\\'), to.TrimEnd('\\'));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    /// <summary>Creates the Start menu .lnk through WScript.Shell — no extra dependency.</summary>
    private static void CreateShortcut()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ShortcutPath)!);

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null) return;

        object? shell = null, link = null;
        try
        {
            shell = Activator.CreateInstance(shellType);
            link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                                          null, shell, new object[] { ShortcutPath });
            if (link is null) return;

            var lt = link.GetType();
            void Set(string prop, object value) =>
                lt.InvokeMember(prop, BindingFlags.SetProperty, null, link, new[] { value });

            Set("TargetPath", InstalledExe);
            Set("WorkingDirectory", InstallDir);
            Set("Description", "Wallpaper of the weeks you have lived");
            Set("IconLocation", InstalledExe + ",0");
            lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
        }
        catch { /* shortcut is a nicety, not a requirement */ }
        finally
        {
            if (link is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
            if (shell is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void RegisterUninstallEntry()
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(UninstallKey);
            if (k is null) return;

            long size = 0;
            try
            {
                foreach (var f in Directory.GetFiles(InstallDir, "*", SearchOption.AllDirectories))
                    size += new FileInfo(f).Length;
            }
            catch { }

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

            k.SetValue("DisplayName", ProductName);
            k.SetValue("DisplayVersion", version);
            k.SetValue("Publisher", ProductName);
            k.SetValue("DisplayIcon", InstalledExe);
            k.SetValue("InstallLocation", InstallDir);
            k.SetValue("UninstallString", $"\"{InstalledExe}\" --uninstall");
            k.SetValue("QuietUninstallString", $"\"{InstalledExe}\" --uninstall --quiet");
            k.SetValue("EstimatedSize", (int)(size / 1024), RegistryValueKind.DWord);
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
        catch { /* not fatal */ }
    }

    /// <summary>A folder cannot delete itself while its exe is running — hand it to cmd.</summary>
    private static void ScheduleSelfDelete(string dir)
    {
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe",
                $"/c timeout /t 3 /nobreak >nul & rmdir /s /q \"{dir}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
    }
}
