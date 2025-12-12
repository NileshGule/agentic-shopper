using AgenticShopper.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Azure AI Foundry LLM provider implementation for production
/// </summary>
public class AzureAIFoundryProvider : ILlmProvider
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

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _modelName,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant." },
                new { role = "user", content = request.Prompt }
            },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/openai/deployments/{_modelName}/chat/completions?api-version=2024-02-15-preview");
        httpRequest.Headers.Add("api-key", _apiKey);
        httpRequest.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var content = result?.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

        return new LlmResponse
        {
            Content = content,
            TokensUsed = result?.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32() ?? 0,
            Model = _modelName
        };
    }

    public async Task<TResponse> GenerateStructuredAsync<TResponse>(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var response = await GenerateAsync(request, cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(response.Content) 
            ?? throw new InvalidOperationException("Failed to deserialize LLM response");
    }
}
