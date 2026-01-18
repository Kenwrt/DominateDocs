namespace DominateDocsSite.OpenAI.Services;

public interface IChatGptService
{
    Task<string> AskChatGptAsync(string userMessage);
}