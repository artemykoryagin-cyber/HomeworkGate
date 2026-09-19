using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;

namespace HomeworkGate.App;

public record UpdateInfo(string Version, string DownloadUrl, string ReleaseNotes);

public class UpdateChecker
{
    // ★ Замени на свой GitHub: "username/HomeworkGate"
    private const string GitHubRepo = "artemykoryagin-cyber/HomeworkGate";
    private const string VersionUrl  = $"https://raw.githubusercontent.com/{GitHubRepo}/main/version.json";

    public const string CurrentVersion = "2.0";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    /// <summary>
    /// Возвращает UpdateInfo если доступна новая версия, иначе null.
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(VersionUrl);
            dynamic? data = JsonConvert.DeserializeObject(json);

            string? remoteVersion = data?.version;
            string? downloadUrl   = data?.download_url;
            string? notes         = data?.notes ?? "";

            if (string.IsNullOrEmpty(remoteVersion) || string.IsNullOrEmpty(downloadUrl))
                return null;

            if (IsNewer(remoteVersion, CurrentVersion))
                return new UpdateInfo(remoteVersion, downloadUrl, notes!);

            return null;
        }
        catch
        {
            // Нет интернета или файл не найден — тихо игнорируем
            return null;
        }
    }

    private static bool IsNewer(string remote, string current)
    {
        if (Version.TryParse(remote, out var r) && Version.TryParse(current, out var c))
            return r > c;
        return string.Compare(remote, current, StringComparison.Ordinal) > 0;
    }
}
