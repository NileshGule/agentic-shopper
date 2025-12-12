namespace AgenticShopper.Core.Interfaces;

/// <summary>
/// LLM provider abstraction for switching between Foundry Local and Azure AI Foundry
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generate text completion from prompt
    /// </summary>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate structured JSON response from prompt
    /// </summary>
    Task<TResponse> GenerateStructuredAsync<TResponse>(LlmRequest request, CancellationToken cancellationToken = default);
}

public class LlmRequest
{
    public required string Prompt { get; set; }
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class LlmResponse
{
    public required string Content { get; set; }
    public int TokensUsed { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
