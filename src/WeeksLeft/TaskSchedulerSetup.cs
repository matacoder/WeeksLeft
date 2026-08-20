using System.Diagnostics;
using System.Text;

namespace WeeksLeft;

/// <summary>
/// Registers a per-user scheduled task instead of keeping a process resident.
/// Triggers: logon, session unlock, and once a day. The task exits in ~30 ms when
/// nothing changed, so a daily trigger costs nothing.
/// </summary>
public static class TaskSchedulerSetup
{
    public const string TaskName = "WeeksLeftWallpaper";
    private const string Sep = "\\";

    public static bool IsInstalled()
    {
        var (code, _) = Run($"/Query /TN \"{TaskName}\"");
        return code == 0;
    }

    /// <summary>Registers the task. <paramref name="exePath"/> overrides which copy it points at.</summary>
    public static (bool ok, string output) Install(AppConfig cfg, string? exePath = null)
    {
        string exe = exePath
            ?? Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "WeeksLeft.exe");
        string user = Environment.UserDomainName + Sep + Environment.UserName;
        bool daily = cfg.UpdateMode != "logon";

        string xml = BuildXml(exe, user, daily);
        string tmp = Path.Combine(Path.GetTempPath(), $"weeksleft-task-{Environment.ProcessId}.xml");
        File.WriteAllText(tmp, xml, new UnicodeEncoding(false, true)); // schtasks wants UTF-16
        try
        {
            var (code, output) = Run($"/Create /TN \"{TaskName}\" /XML \"{tmp}\" /F");
            return (code == 0, output);
        }
        finally { try { File.Delete(tmp); } catch { } }
    }

    public static (bool ok, string output) Uninstall()
    {
        var (code, output) = Run($"/Delete /TN \"{TaskName}\" /F");
        return (code == 0, output);
    }

    private static string BuildXml(string exe, string user, bool daily)
    {
        string dailyTrigger = daily
            ? """
                  <CalendarTrigger>
                    <StartBoundary>2024-01-01T09:05:00</StartBoundary>
                    <Enabled>true</Enabled>
                    <ScheduleByDay><DaysInterval>1</DaysInterval></ScheduleByDay>
                  </CalendarTrigger>
              """
            : "";

        return $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.3" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>Regenerates the WeeksLeft desktop wallpaper. Exits immediately when nothing changed.</Description>
            <URI>\{TaskName}</URI>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger>
              <Enabled>true</Enabled>
              <UserId>{Esc(user)}</UserId>
              <Delay>PT25S</Delay>
            </LogonTrigger>
            <SessionStateChangeTrigger>
              <Enabled>true</Enabled>
              <StateChange>SessionUnlock</StateChange>
              <UserId>{Esc(user)}</UserId>
            </SessionStateChangeTrigger>
        {dailyTrigger}
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>{Esc(user)}</UserId>
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>LeastPrivilege</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <AllowHardTerminate>true</AllowHardTerminate>
            <StartWhenAvailable>true</StartWhenAvailable>
            <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
            <IdleSettings>
              <StopOnIdleEnd>false</StopOnIdleEnd>
              <RestartOnIdle>false</RestartOnIdle>
            </IdleSettings>
            <AllowStartOnDemand>true</AllowStartOnDemand>
            <Enabled>true</Enabled>
            <Hidden>false</Hidden>
            <RunOnlyIfIdle>false</RunOnlyIfIdle>
            <WakeToRun>false</WakeToRun>
            <ExecutionTimeLimit>PT2M</ExecutionTimeLimit>
            <Priority>7</Priority>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{Esc(exe)}</Command>
              <Arguments>--apply</Arguments>
              <WorkingDirectory>{Esc(Path.GetDirectoryName(exe) ?? ".")}</WorkingDirectory>
            </Exec>
          </Actions>
        </Task>
        """;
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static (int code, string output) Run(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(20000);
            return (p.ExitCode, o.Trim());
        }
        catch (Exception ex) { return (-1, ex.Message); }
    }
}
