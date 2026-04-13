using AspNetCoreRateLimit;

namespace AgenticShopper.Coordinator.Configuration;

/// <summary>
/// Configuration class for API rate limiting using AspNetCoreRateLimit library.
/// Provides DDoS protection, brute force prevention, and resource management.
/// </summary>
public static class RateLimitSetup
{
    /// <summary>
    /// Configure IP-based rate limiting for the application.
    /// </summary>
    /// <param name="services">Service collection to add rate limiting services</param>
    /// <param name="configuration">Configuration instance for reading settings</param>
    public static void ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Required for rate limiting
        services.AddMemoryCache();
        
        // Configure IP rate limiting
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        
        // Configure client rate limiting (authenticated users)
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        
        // Load general configuration
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.Configure<ClientRateLimitPolicies>(configuration.GetSection("ClientRateLimitPolicies"));
        
        // Inject counter and rules stores
        services.AddInMemoryRateLimiting();
        
        // Rate limit configuration provider
        services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
    }
    
    /// <summary>
    /// Get default IP rate limit options for production.
    /// </summary>
    public static IpRateLimitOptions GetDefaultIpRateLimitOptions()
    {
        return new IpRateLimitOptions
        {
            EnableEndpointRateLimiting = true,
            StackBlockedRequests = false,
            RealIpHeader = "X-Real-IP",
            ClientIdHeader = "X-ClientId",
            HttpStatusCode = 429, // Too Many Requests
            
            GeneralRules = new List<RateLimitRule>
            {
                // Global rate limit: 100 requests per minute per IP
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                
                // Global rate limit: 1000 requests per hour per IP
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 1000
                }
            },
            
            // Endpoint-specific rate limits
            EndpointWhitelist = new List<string>
            {
                // Health checks and static content not rate limited
                "get:/health",
                "get:/health/ready",
                "get:/swagger*"
            }
        };
    }
    
    /// <summary>
    /// Get default client rate limit options for authenticated users.
    /// </summary>
    public static ClientRateLimitOptions GetDefaultClientRateLimitOptions()
    {
        return new ClientRateLimitOptions
        {
            EnableEndpointRateLimiting = true,
            StackBlockedRequests = false,
            ClientIdHeader = "X-ClientId",
            HttpStatusCode = 429,
            
            GeneralRules = new List<RateLimitRule>
            {
                // Authenticated users: 200 requests per minute
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 200
                },
                
                // Authenticated users: 2000 requests per hour
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 2000
                }
            }
        };
    }
    
    /// <summary>
    /// Get endpoint-specific rate limit policies for critical operations.
    /// </summary>
    public static IpRateLimitPolicies GetEndpointPolicies()
    {
        return new IpRateLimitPolicies
        {
            IpRules = new List<IpRateLimitPolicy>
            {
                // Stricter limits for authentication endpoints
                new IpRateLimitPolicy
                {
                    Ip = "*",
                    Rules = new List<RateLimitRule>
                    {
                        // Login attempts: 5 per 15 minutes (brute force protection)
                        new RateLimitRule
                        {
                            Endpoint = "post:/api/auth/login",
                            Period = "15m",
                            Limit = 5
                        },
                        
                        // Token refresh: 10 per hour
                        new RateLimitRule
                        {
                            Endpoint = "post:/api/auth/refresh",
                            Period = "1h",
                            Limit = 10
                        }
                    }
                },
                
                // Receipt upload limits (resource-intensive OCR operations)
                new IpRateLimitPolicy
                {
                    Ip = "*",
                    Rules = new List<RateLimitRule>
                    {
                        // Receipt uploads: 10 per hour per IP
                        new RateLimitRule
                        {
                            Endpoint = "post:/api/receipts/upload",
                            Period = "1h",
                            Limit = 10
                        },
                        
                        // Receipt uploads: 50 per day per IP
                        new RateLimitRule
                        {
                            Endpoint = "post:/api/receipts/upload",
                            Period = "1d",
                            Limit = 50
                        }
                    }
                },
                
                // Data export limits (prevent data scraping)
                new IpRateLimitPolicy
                {
                    Ip = "*",
                    Rules = new List<RateLimitRule>
                    {
                        // All export endpoints: 100 per day per IP
                        new RateLimitRule
                        {
                            Endpoint = "get:/api/export/*",
                            Period = "1d",
                            Limit = 100
                        },
                        
                        // All export endpoints: 20 per hour per IP
                        new RateLimitRule
                        {
                            Endpoint = "get:/api/export/*",
                            Period = "1h",
                            Limit = 20
                        }
                    }
                }
            }
        };
    }
}
