using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public record EvaluationResult(bool IsPassed, string Feedback, int Score);

public class AiEvaluator
{
    private readonly string _apiKey;
    private static readonly HttpClient _http = new();

    public AiEvaluator(string apiKey) { _apiKey = apiKey; }

    public async Task<EvaluationResult> EvaluateAsync(
        HomeworkTask task,
        string textAnswer,
        List<string> imagePaths,
        List<string> filePaths,
        IProgress<string>? progress = null)
    {
        progress?.Report("Отправляю решение на проверку...");

        var userContentParts = new List<object>();

        userContentParts.Add(new { type = "text", text = $"МОЙ ОТВЕТ:\n{textAnswer}" });

        foreach (var imgPath in imagePaths)
        {
            if (!File.Exists(imgPath)) continue;
            var bytes = await File.ReadAllBytesAsync(imgPath);
            var b64 = Convert.ToBase64String(bytes);
            var ext = System.IO.Path.GetExtension(imgPath).ToLower();
            var mediaType = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            userContentParts.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = mediaType, data = b64 }
            });
        }

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var fname = System.IO.Path.GetFileName(filePath);
                userContentParts.Add(new
                {
                    type = "text",
                    text = $"\n--- Файл: {fname} ---\n{content}\n--- Конец файла ---"
                });
            }
            catch { }
        }

        var systemPrompt = @"Ты — строгий, но справедливый учитель-проверяющий.
Твоя задача — оценить решение ученика по заданию.

ПРАВИЛА ОЦЕНКИ:
1. Оцени, выполнено ли задание по существу — не придирайся к мелочам
2. Если решение правильное или достаточное — засчитай его (passed: true)
3. Если решение неверное, неполное или не по теме — не засчитай (passed: false)
4. Дай конкретную обратную связь: что правильно, что не так, что улучшить

ФОРМАТ ОТВЕТА (только JSON, без markdown):
{""passed"": true/false, ""score"": 0-100, ""feedback"": ""Твой подробный отзыв на русском языке""}";

        var requestBody = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 1000,
            system = systemPrompt,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new List<object>
                    {
                        new { type = "text", text = $"ЗАДАНИЕ:\nНазвание: {task.Title}\nУсловие: {task.Description}" }
                    }.Concat(userContentParts).ToArray()
                }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        progress?.Report("Ожидаю ответ от AI...");

        var response = await _http.SendAsync(req);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new EvaluationResult(false,
                $"Ошибка API: {response.StatusCode}. Проверь API ключ в настройках.", 0);

        dynamic? data = JsonConvert.DeserializeObject(responseJson);
        string? rawText = data?.content?[0]?.text;

        if (string.IsNullOrEmpty(rawText))
            return new EvaluationResult(false, "Не удалось получить ответ от AI.", 0);

        try
        {
            var cleaned = rawText.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned.Split('\n', 2)[1];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();

            dynamic? result = JsonConvert.DeserializeObject(cleaned);
            bool passed = (bool)(result?.passed ?? false);
            int score = (int)(result?.score ?? 0);
            string feedback = (string)(result?.feedback ?? "Нет обратной связи");
            return new EvaluationResult(passed, feedback, score);
        }
        catch
        {
            var upper = rawText.ToUpper();
            bool passed = upper.Contains("PASSED") || upper.Contains("ВЕРНО") || upper.Contains("ПРАВИЛЬНО");
            return new EvaluationResult(passed, rawText, passed ? 80 : 30);
        }
    }
}
