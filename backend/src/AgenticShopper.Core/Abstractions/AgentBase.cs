using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;

namespace AgenticShopper.Core.Abstractions;

/// <summary>
/// Abstract base class for all agents in the multi-agent system using Microsoft Agent Framework patterns.
/// Provides common functionality for agent lifecycle, communication, and execution.
/// </summary>
public abstract class AgentBase
{
    protected readonly ILogger Logger;
    protected readonly IChatClient? ChatClient;

    protected AgentBase(ILogger logger, IChatClient? chatClient = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ChatClient = chatClient;
    }

    /// <summary>
    /// Gets the unique identifier for this agent.
    /// </summary>
    public abstract string AgentId { get; }

    /// <summary>
    /// Gets the human-readable name of this agent.
    /// </summary>
    public abstract string AgentName { get; }

    /// <summary>
    /// Gets the description of what this agent does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Initialize the agent with required resources and configurations.
    /// Called once during agent startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for initialization.</param>
    /// <returns>Task representing the initialization operation.</returns>
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing agent {AgentName} ({AgentId})", AgentName, AgentId);
        await OnInitializeAsync(cancellationToken);
        Logger.LogInformation("Agent {AgentName} initialized successfully", AgentName);
    }

    /// <summary>
    /// Override this method to provide custom initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Execute the agent's primary task with the provided input.
    /// </summary>
    /// <typeparam name="TInput">Type of input data.</typeparam>
    /// <typeparam name="TOutput">Type of output data.</typeparam>
    /// <param name="input">Input data for the agent task.</param>
    /// <param name="cancellationToken">Cancellation token for the execution.</param>
    /// <returns>Task containing the execution result.</returns>
    public async Task<AgentResult<TOutput>> ExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        Logger.LogInformation("Agent {AgentName} starting execution", AgentName);

        try
        {
            var result = await OnExecuteAsync<TInput, TOutput>(input, cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation(
                "Agent {AgentName} completed successfully in {Duration}ms",
                AgentName,
                duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError(
                ex,
                "Agent {AgentName} failed after {Duration}ms: {ErrorMessage}",
                AgentName,
                duration.TotalMilliseconds,
                ex.Message);

            return AgentResult<TOutput>.Failure(
                $"Agent execution failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Convenience alias for ExecuteAsync.
    /// </summary>
    public Task<AgentResult<TOutput>> Execute<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken = default)
        => ExecuteAsync<TInput, TOutput>(input, cancellationToken);

    /// <summary>
    /// Override this method to implement the agent's core execution logic.
    /// </summary>
    protected abstract Task<AgentResult<TOutput>> OnExecuteAsync<TInput, TOutput>(
        TInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cleanup resources when the agent is shutting down.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown.</param>
    public virtual async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Shutting down agent {AgentName}", AgentName);
        await OnShutdownAsync(cancellationToken);
        Logger.LogInformation("Agent {AgentName} shut down successfully", AgentName);
    }

    /// <summary>
    /// Override this method to provide custom shutdown logic.
    /// </summary>
    protected virtual Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a message to another agent in the system.
    /// </summary>
    /// <param name="targetAgentId">ID of the target agent.</param>
    /// <param name="message">Message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual Task SendMessageAsync(
        string targetAgentId,
        object message,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug(
            "Agent {AgentName} sending message to {TargetAgent}",
            AgentName,
            targetAgentId);
        
        // TODO: Implement inter-agent communication via message bus or coordinator
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate input data before processing.
    /// </summary>
    protected virtual bool ValidateInput<TInput>(TInput input, out string? errorMessage)
    {
        if (input == null)
        {
            errorMessage = "Input cannot be null";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Represents the result of an agent execution.
/// </summary>
public class AgentResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }

    public static AgentResult<T> Success(T data, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult<T>
        {
            IsSuccess = true,
            Data = data,
            Metadata = metadata
        };
    }

    public static AgentResult<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new AgentResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
