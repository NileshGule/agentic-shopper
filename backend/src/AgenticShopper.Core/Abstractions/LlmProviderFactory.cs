using AgenticShopper.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Factory for creating LLM provider instances based on configuration
/// </summary>
public class LlmProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmProviderFactory(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public ILlmProvider CreateProvider()
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
