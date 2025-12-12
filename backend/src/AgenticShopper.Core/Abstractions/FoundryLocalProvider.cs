using AgenticShopper.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Foundry Local LLM provider implementation for local development
/// </summary>
public class FoundryLocalProvider : ILlmProvider
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

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _modelName,
            prompt = request.Prompt,
            system = request.SystemPrompt,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/v1/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        var content = result?.RootElement.GetProperty("choices")[0].GetProperty("text").GetString() ?? "";

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
