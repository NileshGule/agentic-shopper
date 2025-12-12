# Phase 0: Research & Technology Decisions

**Feature**: Smart Shopping Pattern Analyzer & Recommender  
**Date**: December 13, 2025  
**Status**: Research Complete

## Research Questions & Decisions

### 1. Microsoft Agent Framework

**Question**: How to implement multi-agent system using Microsoft Agent Framework?

**Decision**: Use Microsoft Agent Framework as the foundation for all agent implementation and orchestration

**Rationale**:
- Microsoft Agent Framework provides built-in LLM abstraction (OpenAI, Azure OpenAI, local models)
- Native support for multi-agent orchestration and communication patterns
- Handles agent lifecycle, state management, and event-driven workflows
- Native C# support with async/await patterns
- Direct integration with Foundry Local and Azure AI Foundry
- Production-ready abstractions for agent coordination, messaging, and monitoring

**Alternatives Considered**:
- **Semantic Kernel**: Lower-level abstractions, requires custom orchestration layer
- **AutoGen**: Python-focused, would require language switch
- **LangChain**: More fragmented ecosystem, less C# support
- **Custom from scratch**: Reinventing abstractions Agent Framework already provides

**Implementation Approach**:
```csharp
// Each agent extends Agent Framework base
public abstract class AgentBase : Agent
{
    protected readonly IAgentRuntime _runtime;
    protected readonly ILlmProvider _llmProvider;
    
    public override async Task<AgentResponse> HandleRequestAsync(AgentRequest request)
    {
        // Agent Framework handles routing, state, and lifecycle
        return await ExecuteAsync(request);
    }
    
    protected abstract Task<AgentResponse> ExecuteAsync(AgentRequest request);
}

// Coordinator orchestrates agents using Agent Framework
public class CoordinatorAgent : Agent
{
    private readonly IAgentRegistry _agentRegistry;
    
    public override async Task<WorkflowResult> HandleRequestAsync(ReceiptUploadRequest request)
    {
        // Agent Framework handles agent discovery and communication
        var receiptAgent = await _agentRegistry.GetAgentAsync("receipt");
        var ocrResult = await receiptAgent.InvokeAsync(new OcrRequest { Image = request.Image });
        
        var categorizationAgent = await _agentRegistry.GetAgentAsync("categorization");
        var categories = await categorizationAgent.InvokeAsync(new CategorizeRequest { Items = ocrResult.Items });
        
        var frequencyAgent = await _agentRegistry.GetAgentAsync("frequency");
        var frequencies = await frequencyAgent.InvokeAsync(new FrequencyRequest { Purchases = categories });
        
        return new WorkflowResult { ... };
    }
}
```

---

### 2. Foundry Local with Azure AI Foundry Migration Path

**Question**: How to use Foundry Local models with seamless migration to Azure AI Foundry?

**Decision**: Implement `ILlmProvider` abstraction with two concrete providers

**Rationale**:
- Foundry Local provides cost-effective development environment
- Azure AI Foundry offers production-grade scaling, managed infrastructure
- Abstraction enables environment-based switching via configuration
- Both support similar model formats (LLaMA, Phi, etc.)

**Implementation Strategy**:
```csharp
// Microsoft Agent Framework provides ILlmProvider abstraction
// We implement concrete providers for each environment

// Foundry Local implementation
public class FoundryLocalProvider : ILlmProvider
{
    private readonly HttpClient _httpClient; // Points to local Foundry endpoint
    private readonly ILogger<FoundryLocalProvider> _logger;
    
    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        // Agent Framework handles prompt formatting, token counting, retries
        var response = await _httpClient.PostAsJsonAsync("/v1/completions", request, ct);
        return await response.Content.ReadFromJsonAsync<LlmResponse>(ct);
    }
}

// Azure AI Foundry implementation
public class AzureAIFoundryProvider : ILlmProvider
{
    private readonly Azure.AI.Foundry.FoundryClient _client;
    private readonly ILogger<AzureAIFoundryProvider> _logger;
    
    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        // Agent Framework handles authentication, throttling, failover
        var response = await _client.GenerateCompletionAsync(request, ct);
        return MapToLlmResponse(response);
    }
}

// Agent Framework configuration
public static class AgentConfiguration
{
    public static IServiceCollection AddAgentFramework(this IServiceCollection services, IConfiguration config)
    {
        services.AddAgentRuntime(options =>
        {
            // Configure LLM provider based on environment
            options.LlmProvider = config["LLM:Provider"] switch
            {
                "FoundryLocal" => new FoundryLocalProvider(config),
                "AzureFoundry" => new AzureAIFoundryProvider(config),
                _ => throw new NotSupportedException()
            };
            
            // Agent Framework handles discovery, communication, monitoring
            options.EnableAgentDiscovery = true;
            options.EnableTelemetry = true;
        });
        
        return services;
    }
}
```

**Configuration**:
```json
// appsettings.Development.json
{
  "LLM": {
    "Provider": "FoundryLocal",
    "Endpoint": "http://localhost:8080",
    "Model": "phi-3-mini"
  }
}

// appsettings.Production.json
{
  "LLM": {
    "Provider": "AzureFoundry",
    "Endpoint": "https://myresource.foundry.azure.com",
    "Model": "gpt-4",
    "ApiKey": "..." // From Key Vault
  }
}
```

---

### 3. OCR Service Selection

**Question**: Which OCR service provides best accuracy for grocery receipts?

**Decision**: Azure AI Document Intelligence (Form Recognizer) as primary, with local fallback evaluation

**Rationale**:
- Azure Document Intelligence has receipt-specific prebuilt models
- Supports layout analysis, table extraction, key-value pairs
- 90%+ accuracy on structured receipts (meets SC-002)
- Processes receipts in <5s (meets performance goal)
- Enterprise-grade (handles 1000+ concurrent users)

**Alternatives Considered**:
- **Tesseract OCR**: Free but lower accuracy (~75-80%), no receipt-specific training
- **Google Cloud Vision**: Similar capabilities but requires GCP account, cross-cloud complexity
- **AWS Textract**: Receipt analysis feature but couples to AWS ecosystem

**Local Development Alternative**:
For local dev without Azure dependencies, evaluate:
- PaddleOCR (open-source, runs locally)
- Acceptance: If accuracy >85% on test receipts, use for offline development

**Implementation**:
```csharp
public interface IOcrService
{
    Task<ReceiptOcrResult> ProcessReceiptAsync(Stream image);
}

public class AzureDocumentIntelligenceOcrService : IOcrService
{
    private readonly DocumentAnalysisClient _client;
    
    public async Task<ReceiptOcrResult> ProcessReceiptAsync(Stream image)
    {
        var operation = await _client.AnalyzeDocumentFromUriAsync(
            WaitUntil.Completed, 
            "prebuilt-receipt", 
            imageUri);
            
        var result = operation.Value;
        return MapToReceiptOcrResult(result);
    }
}
```

---

### 4. Real-Time Collaborative Editing

**Question**: How to implement real-time list synchronization across family members (FR-040)?

**Decision**: SignalR for WebSocket-based real-time sync

**Rationale**:
- Native ASP.NET Core integration
- Automatic fallback to long polling if WebSockets unavailable
- Built-in support for scaling via Azure SignalR Service
- <100ms latency achievable for list updates
- TypeScript client library for React frontend

**Alternatives Considered**:
- **Polling**: High latency, inefficient bandwidth usage
- **Server-Sent Events (SSE)**: Unidirectional, requires separate channel for client→server updates
- **GraphQL Subscriptions**: Adds complexity, requires Apollo/Relay setup

**Implementation**:
```csharp
// Backend hub
public class ShoppingListHub : Hub
{
    public async Task JoinList(string listId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, listId);
    }
    
    public async Task UpdateItem(string listId, ShoppingListItem item)
    {
        // Save to database
        await _repository.UpdateItemAsync(item);
        
        // Broadcast to all connected clients viewing this list
        await Clients.Group(listId).SendAsync("ItemUpdated", item);
    }
}

// Frontend (React)
const connection = new HubConnectionBuilder()
    .withUrl("/hubs/shopping-list")
    .withAutomaticReconnect()
    .build();

connection.on("ItemUpdated", (item) => {
    setListItems(prev => prev.map(i => i.id === item.id ? item : i));
});
```

---

### 5. Store Catalog Integration (Coles, Woolworths)

**Question**: How to retrieve promotional pricing data from store catalogs?

**Decision**: Web scraping + API integration where available, with caching layer

**Rationale**:
- Neither Coles nor Woolworths provide official public APIs
- Weekly promotions have predictable patterns (Wednesday updates)
- Caching reduces scraping frequency (FR-027: weekly updates)
- Build adapter pattern to support future official APIs

**Research Findings**:
- **Coles**: Website structure uses AJAX for catalog data (JSON responses scrapable)
- **Woolworths**: Similar architecture, catalog endpoint at `/api/v1/products/specials`
- **Legal**: Scraping for personal/research use generally acceptable; verify ToS compliance
- **Reliability**: Add retry logic + stale data handling (edge case: catalog unavailable)

**Implementation**:
```csharp
public interface IStoreCatalogService
{
    Task<List<Promotion>> GetCurrentPromotionsAsync(string storeName);
    Task<ProductPrice> GetProductPriceAsync(string productName, string storeName);
}

public class ColesCatalogService : IStoreCatalogService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    
    public async Task<List<Promotion>> GetCurrentPromotionsAsync(string storeName)
    {
        var cacheKey = $"coles-promotions-{DateTime.UtcNow:yyyy-MM-dd}";
        
        if (_cache.TryGetValue(cacheKey, out List<Promotion> cached))
            return cached;
        
        // Scrape current week's catalog
        var promotions = await ScrapePromotionsAsync();
        
        _cache.Set(cacheKey, promotions, TimeSpan.FromDays(7));
        return promotions;
    }
    
    private async Task<List<Promotion>> ScrapePromotionsAsync()
    {
        var response = await _httpClient.GetAsync("/api/specials");
        var json = await response.Content.ReadAsStringAsync();
        // Parse JSON into Promotion objects
    }
}
```

**Fallback Strategy**:
- If scraping fails → Use last known prices (mark as stale)
- If product not found → Omit promotion indicator (edge case handled)

---

### 6. Deployment Architecture

**Question**: How to support local, Kubernetes, Azure Container Apps, and Foundry Agent Service?

**Decision**: Containerized agents with environment-specific orchestration manifests

**Rationale**:
- Docker provides consistent runtime across environments
- Each agent as separate container enables independent scaling
- Kubernetes for on-prem/self-managed cloud
- Azure Container Apps for managed Azure deployment
- Foundry Agent Service integration for AI-native hosting

**Deployment Strategies**:

| Environment | Orchestration | Configuration |
|-------------|---------------|---------------|
| **Local Dev** | Docker Compose | All agents + PostgreSQL + Foundry Local on localhost |
| **Kubernetes** | K8s manifests | Deployments per agent, services for discovery, ingress for external access |
| **Azure Container Apps** | Bicep templates | Container Apps per agent, managed PostgreSQL, Azure Blob, SignalR Service |
| **Foundry Agent Service** | Agent definitions | Agents registered as Foundry services, managed lifecycle |

**Docker Compose (Local)**:
```yaml
version: '3.8'
services:
  coordinator:
    build:
      context: ./backend
      dockerfile: deployment/docker/Dockerfile.coordinator
    ports:
      - "5000:80"
    environment:
      - LLM__Provider=FoundryLocal
      - LLM__Endpoint=http://foundry-local:8080
    depends_on:
      - postgres
      - foundry-local
  
  receipt-agent:
    build:
      context: ./backend
      dockerfile: deployment/docker/Dockerfile.agent
    environment:
      - AGENT_TYPE=Receipt
      - OCR__Provider=Local # PaddleOCR for local dev
  
  # ... other agents ...
  
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: agentic_shopper
  
  foundry-local:
    image: foundry/local:latest
    volumes:
      - ./models:/models
```

**Kubernetes**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: coordinator
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: coordinator
        image: agenticshopper/coordinator:latest
        env:
        - name: LLM__Provider
          valueFrom:
            configMapKeyRef:
              name: llm-config
              key: provider
```

**Azure Container Apps (Bicep)**:
```bicep
resource coordinatorApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'coordinator'
  properties: {
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
    }
    template: {
      containers: [
        {
          name: 'coordinator'
          image: 'agenticshopper.azurecr.io/coordinator:latest'
          env: [
            {
              name: 'LLM__Provider'
              value: 'AzureFoundry'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}
```

---

### 7. Agent Communication Patterns

**Question**: Should agents communicate synchronously (HTTP) or asynchronously (message queue)?

**Decision**: Hybrid approach
- **Synchronous (HTTP REST)**: Coordinator → Agent for request/response workflows
- **Asynchronous (Azure Service Bus/RabbitMQ)**: Background tasks (weekly promotion refresh, budget alerts)

**Rationale**:
- Most workflows are request-driven (user uploads receipt → process immediately)
- Some tasks are time-triggered (weekly promotion sync) or event-driven (budget threshold)
- Synchronous APIs meet <200ms p95 response time requirements
- Async messaging handles long-running tasks without blocking

**Patterns**:

```csharp
// Synchronous: Receipt upload workflow
public class ReceiptController : ControllerBase
{
    private readonly IAgentCoordinator _coordinator;
    
    [HttpPost]
    public async Task<IActionResult> UploadReceipt(IFormFile file)
    {
        var result = await _coordinator.ProcessReceiptWorkflowAsync(file.OpenReadStream());
        return Ok(result); // Returns in <5s per requirement
    }
}

// Asynchronous: Weekly promotion refresh
public class PromotionRefreshJob : IHostedService
{
    private readonly IMessageBus _messageBus;
    
    public async Task StartAsync(CancellationToken ct)
    {
        // Schedule weekly job
        _timer = new Timer(async _ =>
        {
            await _messageBus.PublishAsync(new RefreshPromotionsCommand());
        }, null, TimeSpan.Zero, TimeSpan.FromDays(7));
    }
}

// Price agent listens for refresh commands
public class PriceAgentMessageHandler
{
    public async Task HandleAsync(RefreshPromotionsCommand command)
    {
        await _priceAgent.RefreshAllPromotionsAsync();
    }
}
```

---

## Technology Stack Summary

| Category | Technology | Purpose |
|----------|-----------|---------|
| **Language** | C# 12 (.NET 10) | Backend agents, APIs, coordinator |
| **AI Framework** | Microsoft Agent Framework | Multi-agent orchestration, LLM abstraction, agent lifecycle management |
| **LLM (Dev)** | Foundry Local | Local model hosting during development |
| **LLM (Prod)** | Azure AI Foundry | Production-grade managed LLM service |
| **OCR (Prod)** | Azure AI Document Intelligence | Receipt processing with prebuilt models |
| **OCR (Dev)** | PaddleOCR (evaluation) | Local OCR for offline development |
| **Frontend** | React 18 + TypeScript | User interface, SPA |
| **API Protocol** | REST (OpenAPI 3.0) | Inter-agent + frontend-backend communication |
| **Real-time Sync** | SignalR | WebSocket-based collaborative editing |
| **Database** | PostgreSQL 15+ | Relational data (receipts, products, lists, budgets) |
| **Blob Storage** | Azure Blob Storage | Receipt images (10MB max) |
| **Caching** | Redis (or in-memory) | Promotion data, session state |
| **Message Bus** | Azure Service Bus / RabbitMQ | Async agent communication, background jobs |
| **Testing (Backend)** | xUnit + Moq + Testcontainers | Unit, integration, contract tests |
| **Testing (Frontend)** | Jest + React Testing Library | Component, integration tests |
| **Containerization** | Docker | Agent packaging, consistent runtime |
| **Orchestration (Local)** | Docker Compose | Local multi-container development |
| **Orchestration (K8s)** | Kubernetes | On-prem/self-managed deployment |
| **Orchestration (Azure)** | Azure Container Apps | Managed container orchestration |
| **Agent Hosting** | Foundry Agent Service | AI-native agent deployment |
| **CI/CD** | GitHub Actions | Automated build, test, deploy |
| **API Documentation** | Swagger/OpenAPI | Auto-generated API docs |
| **Monitoring** | Application Insights | Telemetry, performance tracking |

---

## Next Steps (Phase 1)

1. **Data Model Design** → `data-model.md`
   - Entity relationship diagrams
   - Database schema definitions
   - Model classes with annotations

2. **API Contracts** → `contracts/` directory
   - OpenAPI specs for each agent API
   - Request/response schemas
   - Error handling specifications

3. **Developer Quickstart** → `quickstart.md`
   - Environment setup instructions
   - Running locally with Docker Compose
   - Testing guide
   - Deployment to different environments

4. **Agent Context Update**
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
   - Add technology stack to agent-specific context

---

## Research Validation Checklist

- [x] Microsoft Agent Framework approach defined
- [x] Foundry Local + Azure AI Foundry migration strategy clear
- [x] OCR service selected with fallback option
- [x] Real-time collaboration technology chosen
- [x] Store catalog integration approach researched
- [x] Multi-environment deployment architecture designed
- [x] Agent communication patterns established
- [x] Technology stack complete and compatible
- [x] All performance/scalability requirements addressable
- [x] All functional requirements technically feasible

**Status**: ✅ **READY FOR PHASE 1 (Design & Contracts)**
