using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace budget_planner.Controllers
{
    public class AiController : Controller
    {
        private readonly IConfiguration _configuration;

        // Dependency Injection ilə IConfiguration qəbul edilir
        public AiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> AskAi([FromBody] AiChatRequest request)
        {
            // 1. Daxil olan mesajın yoxlanılması
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, message = "Lütfən sualınızı daxil edin." });
            }

            try
            {
                // 2. API Key appsettings.json-dan oxunur
                string geminiApiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(geminiApiKey))
                {
                    return Json(new { success = false, message = "API Key appsettings.json konfiqurasiyasında tapılmadı." });
                }

                // 3. Gemini API Endpoint
                string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

                // 4. Sorğu strukturu (JSON Payload)
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = "Sən 'Büdcəm' adlı şəxsi maliyyə idarəetmə tətbiqinin ağıllı maliyyə məsləhətçisisən. İstifadəçiyə şəxsi büdcə planlaması və maliyyə mövzularında aydın, nəzakətli, qısa və faydalı məsləhətlər ver." }
                        }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = request.Message }
                            }
                        }
                    }
                };

                using var httpClient = new HttpClient();

                // API Key Header-də göndərilir
                httpClient.DefaultRequestHeaders.Add("X-goog-api-key", geminiApiKey);

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // DEBUG
                Console.WriteLine($"URL: {geminiApiUrl}");
                Console.WriteLine($"Request: {jsonContent}");

                // Sorğunun göndərilməsi
                var response = await httpClient.PostAsync(geminiApiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                // DEBUG
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseString}");

                // 7. Xəta kontrolu
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Google API Xətası: " + responseString });
                }

                // 8. Cavabın parse edilməsi
                using var doc = JsonDocument.Parse(responseString);
                string aiAnswer = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Cavab hazırlana bilmədi.";

                return Json(new { success = true, answer = aiAnswer.Trim() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "C# Sistem xətası: " + ex.Message });
            }
        }
    }

    public class AiChatRequest
    {
        public string Message { get; set; }
    }
}
