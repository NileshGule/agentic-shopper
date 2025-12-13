using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Azure AI Foundry chat client implementation for production using Microsoft.Extensions.AI
/// </summary>
public class AzureAIFoundryProvider : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelName;

    public AzureAIFoundryProvider(HttpClient httpClient, string endpoint, string apiKey, string modelName = "gpt-4")
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _apiKey = apiKey;
        _modelName = modelName;
    }

    public ChatClientMetadata Metadata => new("AzureAIFoundry", new Uri(_endpoint), _modelName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.Select(m => new
        {
            role = m.Role.Value.ToLowerInvariant(),
            content = m.Text
        }).ToArray();

        var payload = new
        {
            model = options?.ModelId ?? _modelName,
            messages,
            temperature = options?.Temperature ?? 0.7,
            max_tokens = options?.MaxOutputTokens ?? 1000
        };

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint}/openai/deployments/{_modelName}/chat/completions?api-version=2024-02-15-preview");
        httpRequest.Headers.Add("api-key", _apiKey);
        httpRequest.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var content = result?.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
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
        // Azure AI Foundry supports streaming, but implementing basic version
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
