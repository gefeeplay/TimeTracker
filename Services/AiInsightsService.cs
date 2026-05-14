using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TimeTracker.Models;

namespace TimeTracker.Services;

public class AiInsightsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string MODEL =
        "deepseek/deepseek-v4-flash:free";
    private const string ENDPOINT =
        "https://openrouter.ai/api/v1/chat/completions";

    public AiInsightsService()
    {
        _apiKey = Environment.GetEnvironmentVariable(
           "OPENROUTER_API_KEY")
           ?? throw new InvalidOperationException(
               "OPENROUTER_API_KEY not found");

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        _httpClient.DefaultRequestHeaders.Add(
            "X-Title",
            "TimeTracker");
    }

    public async Task<string> GenerateTipsAsync(DashboardAiContext context)
    {
        try {
            var prompt =
            $"""
                Ты аналитик цифровой активности пользователя.
                На основе статистики дай короткий совет
                (максимум 2 предложения).
                Правила: 
                - Пиши только по-русски
                - Без markdown
                - Без списков
                - Максимум 2 предложения
                - Не придумывай данные
                - Совет должен быть полезным"Правила:   
                Данные:
                - Общее экранное время:  {context.TotalSecondsToday / 60.0:F1} минут
                - Самое используемое приложение:  {context.MostFrequentApp}
                - Время в приложении:  {context.MostFrequentTime / 60} минут
                - Переключения окон:  {context.WindowSwitches}
                - Выполнение дневной цели:  {context.DailyGoalPercent:F0}%
                - Цель превышена:  {(context.GoalExceeded ? "Да" : "Нет")}
                """;

            var body = new
            {
                model = MODEL,
                messages = new[]
                        {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
                temperature = 0.7,
                max_tokens = 400
            };

            var json = JsonSerializer.Serialize(body);

            var response = await _httpClient.PostAsync(
                ENDPOINT,
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "AI-сервис временно перегружен.";
            }

            if (!response.IsSuccessStatusCode)
            {
                return $"Ошибка AI: {(int)response.StatusCode}";
            }
            ;

            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(responseJson);

            using var doc = JsonDocument.Parse(responseJson);

            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");

            string? text = null;

            if (message.TryGetProperty("content", out var contentElement))
            {
                text = contentElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return "Не удалось сформировать рекомендацию.";
            }

            return text.Trim();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            return "AI-рекомендации временно недоступны.";
        }
    }
    
}