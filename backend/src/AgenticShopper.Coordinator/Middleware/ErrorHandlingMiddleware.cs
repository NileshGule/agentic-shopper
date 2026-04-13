using System.Net;
using System.Text.Json;

namespace AgenticShopper.Coordinator.Middleware;

/// <summary>
/// Global error handling middleware to catch unhandled exceptions
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception chain for debugging
            var innerMessage = ex.InnerException?.Message;
            _logger.LogError(ex,
                "An unhandled exception occurred: {Message}. Inner: {InnerMessage}",
                ex.Message,
                innerMessage ?? "(none)");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                code = HttpStatusCode.BadRequest;
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                break;
            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                break;
        }

        result = JsonSerializer.Serialize(new
        {
            error = exception.Message,
            statusCode = (int)code
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}
