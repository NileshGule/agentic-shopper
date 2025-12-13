using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Foundry Local chat client implementation for local development using Microsoft.Extensions.AI
/// </summary>
public class FoundryLocalProvider : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelName;

    public FoundryLocalProvider(HttpClient httpClient, string endpoint, string modelName = "phi-3-mini")
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _modelName = modelName;
    }

    public ChatClientMetadata Metadata => new("FoundryLocal", new Uri(_endpoint), _modelName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var systemMessage = chatMessages.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? "You are a helpful assistant.";
        var userMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

        var payload = new
        {
            model = options?.ModelId ?? _modelName,
            prompt = userMessage,
            system = systemMessage,
            temperature = options?.Temperature ?? 0.7,
            max_tokens = options?.MaxOutputTokens ?? 1000
        };

        var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/v1/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var content = result?.RootElement.GetProperty("choices")[0].GetProperty("text").GetString() ?? "";
        var tokensUsed = result?.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32() ?? 0;

        return new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, content) })
        {
            Usage = new UsageDetails { TotalTokenCount = tokensUsed },
            ModelId = _modelName
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Foundry Local typically doesn't support streaming, fall back to non-streaming
        var completion = await GetResponseAsync(chatMessages, options, cancellationToken);
        var lastMessage = completion.Messages.LastOrDefault();
        if (lastMessage != null)
        {
            yield return new ChatResponseUpdate
            {
                Contents = [new TextContent(lastMessage.Text ?? string.Empty)],
                Role = ChatRole.Assistant
            };
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
