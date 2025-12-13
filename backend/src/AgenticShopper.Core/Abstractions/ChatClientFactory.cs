using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Factory for creating chat client instances based on configuration (Microsoft Agent Framework pattern)
/// </summary>
public class ChatClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatClientFactory(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public IChatClient CreateChatClient()
    {
        var provider = _configuration["LLM:Provider"] ?? "FoundryLocal";
        var endpoint = _configuration["LLM:Endpoint"] ?? throw new InvalidOperationException("LLM endpoint not configured");
        var model = _configuration["LLM:Model"] ?? "phi-3-mini";

        var httpClient = _httpClientFactory.CreateClient("LlmClient");

        return provider switch
        {
            "FoundryLocal" => new FoundryLocalProvider(httpClient, endpoint, model),
            "AzureFoundry" => new AzureAIFoundryProvider(
                httpClient, 
                endpoint, 
                _configuration["LLM:ApiKey"] ?? throw new InvalidOperationException("Azure API key not configured"),
                model),
            _ => throw new NotSupportedException($"LLM provider '{provider}' is not supported")
        };
    }
}
