using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public record EvaluationResult(bool IsPassed, string Feedback, int Score);

public class AiEvaluator
{
    // ★ Бесплатный ключ: https://aistudio.google.com → Get API key
    private const string ApiKey = "AQ.Ab8RN6IDdA98MUtp3rChR4jnd-6QvnxefX_3Hv8c7d3MzB0PVQ";
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private static readonly HttpClient _http = new();

    // Оставляем конструктор совместимым (ключ из настроек игнорируется если вшит)
    public AiEvaluator(string? apiKey = null) { }

    public async Task<EvaluationResult> EvaluateAsync(
        HomeworkTask task,
        string textAnswer,
        List<string> imagePaths,
        List<string> filePaths,
        IProgress<string>? progress = null)
    {
        progress?.Report("Отправляю решение на проверку...");

        // Собираем текст запроса
        var sb = new StringBuilder();
        sb.AppendLine("Ты — строгий, но справедливый учитель-проверяющий.");
        sb.AppendLine("Оцени решение ученика по заданию.");
        sb.AppendLine();
        sb.AppendLine("ПРАВИЛА:");
        sb.AppendLine("1. Если решение правильное или достаточное — засчитай (passed: true)");
        sb.AppendLine("2. Если неверное или неполное — не засчитай (passed: false)");
        sb.AppendLine("3. Дай конкретную обратную связь на русском");
        sb.AppendLine();
        sb.AppendLine("ФОРМАТ ОТВЕТА (только JSON, без markdown):");
        sb.AppendLine("{\"passed\": true/false, \"score\": 0-100, \"feedback\": \"...\"}");
        sb.AppendLine();
        sb.AppendLine($"ЗАДАНИЕ: {task.Title}");
        sb.AppendLine($"УСЛОВИЕ: {task.Description}");
        sb.AppendLine();
        sb.AppendLine($"ОТВЕТ УЧЕНИКА: {textAnswer}");

        // Прикреплённые файлы — добавляем как текст
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var fname = Path.GetFileName(filePath);
                sb.AppendLine($"\n--- Файл: {fname} ---\n{content}\n--- Конец файла ---");
            }
            catch { }
        }

        // Строим parts для Gemini
        var parts = new List<object>
        {
            new { text = sb.ToString() }
        };

        // Изображения
        foreach (var imgPath in imagePaths)
        {
            if (!File.Exists(imgPath)) continue;
            try
            {
                var bytes = await File.ReadAllBytesAsync(imgPath);
                var b64 = Convert.ToBase64String(bytes);
                var ext = Path.GetExtension(imgPath).ToLower();
                var mime = ext switch
                {
                    ".png"  => "image/png",
                    ".gif"  => "image/gif",
                    ".webp" => "image/webp",
                    _       => "image/jpeg"
                };
                parts.Add(new
                {
                    inline_data = new { mime_type = mime, data = b64 }
                });
            }
            catch { }
        }

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = parts.ToArray() }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1000
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var url  = $"{ApiUrl}?key={ApiKey}";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        progress?.Report("Ожидаю ответ от AI...");

        var response     = await _http.SendAsync(req);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new EvaluationResult(false,
                $"Ошибка API: {response.StatusCode}. Проверь API ключ в AiEvaluator.cs", 0);

        dynamic? data    = JsonConvert.DeserializeObject(responseJson);
        string?  rawText = data?.candidates?[0]?.content?.parts?[0]?.text;

        if (string.IsNullOrEmpty(rawText))
            return new EvaluationResult(false, "Не удалось получить ответ от AI.", 0);

        try
        {
            var cleaned = rawText.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned.Split('\n', 2)[1];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();

            dynamic? result  = JsonConvert.DeserializeObject(cleaned);
            bool     passed  = (bool)(result?.passed  ?? false);
            int      score   = (int)(result?.score    ?? 0);
            string   feedback = (string)(result?.feedback ?? "Нет обратной связи");
            return new EvaluationResult(passed, feedback, score);
        }
        catch
        {
            var upper  = rawText.ToUpper();
            bool passed = upper.Contains("PASSED") || upper.Contains("ВЕРНО") || upper.Contains("ПРАВИЛЬНО");
            return new EvaluationResult(passed, rawText, passed ? 80 : 30);
        }
    }
}
