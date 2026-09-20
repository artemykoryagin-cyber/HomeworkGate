using System.IO;
using Newtonsoft.Json;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public class StateManager
{
    // Use ProgramData so the Guardian Windows Service (running as SYSTEM) can read state too
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "HomeworkGate");

    private static readonly string StateFile = Path.Combine(DataDir, "state.json");

    public AppState State { get; private set; }

    public event Action? StateChanged;

    public StateManager()
    {
        Directory.CreateDirectory(DataDir);
        State = Load();
    }

    private AppState Load()
    {
        try
        {
            if (File.Exists(StateFile))
            {
                var json = File.ReadAllText(StateFile);
                return JsonConvert.DeserializeObject<AppState>(json) ?? new AppState();
            }
        }
        catch { }
        return new AppState();
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(State, Formatting.Indented);
            File.WriteAllText(StateFile, json);
            StateChanged?.Invoke();
        }
        catch { }
    }

    public void AddTask(HomeworkTask task) { State.Tasks.Add(task); Save(); }
    public void UpdateTask(HomeworkTask task)
    {
        var idx = State.Tasks.FindIndex(t => t.Id == task.Id);
        if (idx >= 0) State.Tasks[idx] = task;
        Save();
    }
    public void RemoveTask(Guid id) { State.Tasks.RemoveAll(t => t.Id == id); Save(); }
    public void AddBlockedApp(BlockedApp app) { State.BlockedApps.Add(app); Save(); }
    public void RemoveBlockedApp(Guid id) { State.BlockedApps.RemoveAll(a => a.Id == id); Save(); }
    public void SetApiKey(string key) { State.ApiKey = key; Save(); }
}
