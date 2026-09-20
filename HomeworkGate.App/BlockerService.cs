using System.Diagnostics;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

/// <summary>
/// Secondary in-process blocker that runs in the UI.
/// The primary blocker is the Guardian Windows Service which cannot be killed by users.
/// This one catches anything the service might miss during startup.
/// </summary>
public class BlockerService
{
    private readonly StateManager _state;

    public BlockerService(StateManager state)
    {
        _state = state;
    }

    public void EnforceBlock()
    {
        if (!_state.State.IsLocked) return;

        foreach (var blocked in _state.State.BlockedApps)
        {
            try
            {
                var procName = System.IO.Path.GetFileNameWithoutExtension(blocked.ProcessName);
                var procs = Process.GetProcessesByName(procName);

                foreach (var proc in procs)
                {
                    try
                    {
                        // Match by full executable path when possible (avoids false kills)
                        string? mainModulePath = null;
                        try { mainModulePath = proc.MainModule?.FileName; } catch { }

                        bool shouldKill = mainModulePath != null
                            ? string.Equals(mainModulePath, blocked.ExecutablePath,
                                StringComparison.OrdinalIgnoreCase)
                            : true;

                        if (shouldKill)
                            proc.Kill(entireProcessTree: true);
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }
            }
            catch { }
        }
    }
}
