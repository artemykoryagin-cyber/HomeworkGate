using Newtonsoft.Json;

namespace HomeworkGate.Shared;

// Renamed from TaskStatus to avoid conflict with System.Threading.Tasks.TaskStatus
public enum HwStatus { Pending, InReview, Completed, Failed }

public class HomeworkTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsMandatory { get; set; } = true;
    public HwStatus Status { get; set; } = HwStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string? LastFeedback { get; set; }
    public int AttemptCount { get; set; } = 0;
}

public class BlockedApp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string ProcessName { get; set; } = "";
}

public class AppState
{
    public List<HomeworkTask> Tasks { get; set; } = new();
    public List<BlockedApp> BlockedApps { get; set; } = new();
    public string ApiKey { get; set; } = "";
    public bool IsLocked => Tasks.Any(t => t.IsMandatory && t.Status != HwStatus.Completed);
}
