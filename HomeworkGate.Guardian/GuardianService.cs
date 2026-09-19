using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HomeworkGate.Shared;

namespace HomeworkGate.Guardian;

/// <summary>
/// Windows Service that runs as SYSTEM/LocalSystem with elevated privileges.
/// Blocks configured processes every 2 seconds regardless of whether
/// the main UI is running. Cannot be killed by standard users.
/// </summary>
public class GuardianService : BackgroundService
{
    private static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "HomeworkGate", "state.json");

    private readonly ILogger<GuardianService> _logger;

    public GuardianService(ILogger<GuardianService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HomeworkGate Guardian started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnforceBlock();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during block enforcement.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private void EnforceBlock()
    {
        var state = LoadState();
        if (state == null || !state.IsLocked) return;

        foreach (var blocked in state.BlockedApps)
        {
            try
            {
                var procName = Path.GetFileNameWithoutExtension(blocked.ProcessName);
                var procs = Process.GetProcessesByName(procName);

                foreach (var proc in procs)
                {
                    try
                    {
                        // Try to match by full path first (more precise)
                        string? mainModulePath = null;
                        try { mainModulePath = proc.MainModule?.FileName; } catch { }

                        bool shouldKill = mainModulePath != null
                            ? string.Equals(mainModulePath, blocked.ExecutablePath,
                                StringComparison.OrdinalIgnoreCase)
                            : true; // fallback: name match is enough

                        if (shouldKill)
                        {
                            proc.Kill(entireProcessTree: true);
                            _logger.LogInformation("Killed blocked process: {Name} (PID {Pid})",
                                proc.ProcessName, proc.Id);
                        }
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }
            }
            catch { }
        }
    }

    private AppState? LoadState()
    {
        try
        {
            if (!File.Exists(StateFile)) return null;
            var json = File.ReadAllText(StateFile);
            return JsonConvert.DeserializeObject<AppState>(json);
        }
        catch { return null; }
    }
}
