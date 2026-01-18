using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DominateDocsSite.OpenAI.Services;

public class ChatGptService : IChatGptService
{
    private readonly ILogger<ChatGptService> logger;

    private readonly IOptions<OpenApiConfigOptions> options;
    private readonly HttpClient httpClient;
    private readonly string apiKey;

    public ChatGptService(IOptions<OpenApiConfigOptions> options, ILogger<ChatGptService> logger)
    {
        this.logger = logger;

        this.options = options;

        apiKey = options.Value.KeyValue;

        httpClient = new HttpClient();
    }

    public async Task<string> AskChatGptAsync(string userMessage)
    {
        string? responseJson = "";

        try
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
            {
                new { role = "user", content = userMessage }
            }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(requestMessage);

            response.EnsureSuccessStatusCode();

            responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return responseJson;
    }
}