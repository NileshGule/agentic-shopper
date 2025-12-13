````markdown
# Implementation Plan: Smart Shopping Pattern Analyzer & Recommender

**Branch**: `001-shopping-analyzer` | **Date**: December 13, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-shopping-analyzer/spec.md`

## Summary

Build an AI-powered shopping pattern analyzer using a multi-agent architecture. The system processes grocery receipts via OCR, categorizes products, tracks purchase frequencies, and generates intelligent shopping lists with promotional price comparisons across stores (Coles, Woolworths). Family members collaborate on shared lists and budgets. The implementation uses Microsoft Agentic Framework with autonomous agents coordinated via a central coordinator, deployable to multiple environments (local, Kubernetes, Azure, Foundry Agent Service).

**Core Value**: Automate weekly/monthly shopping list creation by analyzing historical patterns, optimizing savings through promotional analysis, and providing spending insights via visual dashboards.

## Technical Context

**Language/Version**: C# 12 (.NET 10)  
**Primary Framework**: Microsoft Agent Framework
**AI/LLM**: Foundry Local models (with abstraction layer for Azure AI Foundry migration)
**Frontend**: React 18.x with TypeScript  
**Backend Architecture**: Multi-agent system with autonomous agents + Microsoft Agent Framework Workflow orchestration
**API Design**: REST API-first approach (OpenAPI/Swagger)  
**Orchestration**: Microsoft Agent Framework WorkflowBuilder with graph-based agent coordination
**Storage**: PostgreSQL 15+ (receipts, products, frequencies) + Azure Blob Storage (receipt images)  
**OCR Service**: Azure AI Document Intelligence (with local alternative evaluation)  
**Testing**: xUnit for backend, Jest/React Testing Library for frontend  
**Target Platform**: Cross-platform deployment (local dev, Docker, Kubernetes, Azure Container Apps, Foundry Agent Service)
**Project Type**: Web application (frontend + backend APIs + agent orchestration)  
**Performance Goals**: 
- OCR processing: <5s per receipt
- Shopping list generation: <2s
- API response time: <200ms p95
- Support 1000+ concurrent users  
**Constraints**: 
- Receipt images <10MB
- Real-time collaborative editing (<100ms sync latency)
- Weekly promotion data refresh
- 90%+ OCR accuracy for clear receipts  
**Scale/Scope**: 
- 7 user stories (P1-P3 priority)
- 51 functional requirements
- 10 data entities
- Multiple autonomous agents (Receipt Agent, Categorization Agent, Frequency Agent, List Generator Agent, Price Comparison Agent, Budget Agent)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ⚠️ CONSTITUTION FILE IS PLACEHOLDER - No specific gates defined yet

**Assumed Core Principles for This Feature**:
1. **Agent-First Architecture**: Every major capability implemented as autonomous agent
2. **API-First Design**: All agent capabilities exposed via REST APIs
3. **Test-First Development**: TDD with unit + integration tests before implementation
4. **Model Abstraction**: LLM provider abstraction enabling local↔cloud switching
5. **Environment Portability**: Single codebase deployable across multiple runtimes

**Re-evaluation After Phase 1**: Will validate agent boundaries, API contracts, and deployment architecture against final constitution.

## Project Structure

### Documentation (this feature)

```text
specs/001-shopping-analyzer/
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0: Technology research & decisions
├── data-model.md        # Phase 1: Entity models & relationships
├── quickstart.md        # Phase 1: Developer onboarding guide
├── contracts/           # Phase 1: OpenAPI specs for all APIs
│   ├── receipt-agent-api.yaml
│   ├── categorization-agent-api.yaml
│   ├── frequency-agent-api.yaml
│   ├── list-generator-agent-api.yaml
│   ├── price-agent-api.yaml
│   ├── budget-agent-api.yaml
│   └── coordinator-api.yaml
└── tasks.md             # Phase 2: Development tasks (/speckit.tasks)
```

### Source Code (repository root)

```text
agentic-shopper/
├── backend/
│   ├── src/
│   │   ├── AgenticShopper.Coordinator/          # Workflow orchestration using Agent Framework
│   │   │   ├── Program.cs
│   │   │   ├── Controllers/
│   │   │   ├── Workflows/
│   │   │   │   ├── ReceiptProcessingWorkflow.cs    # OCR → Parse → Categorize → Frequency
│   │   │   │   ├── ListGenerationWorkflow.cs       # Frequency Analysis → Price Comparison → List Creation
│   │   │   │   └── BudgetAnalysisWorkflow.cs       # Spending Analysis → Budget Alerts
│   │   │   ├── Services/
│   │   │   └── appsettings.json
│   │   ├── AgenticShopper.Agents.Receipt/       # Receipt processing agent
│   │   │   ├── ReceiptAgent.cs
│   │   │   ├── Services/
│   │   │   │   ├── OcrService.cs
│   │   │   │   └── ReceiptParser.cs
│   │   │   └── Models/
│   │   ├── AgenticShopper.Agents.Categorization/ # Product categorization agent
│   │   │   ├── CategorizationAgent.cs
│   │   │   ├── Services/
│   │   │   │   └── CategoryClassifier.cs
│   │   │   └── Prompts/
│   │   ├── AgenticShopper.Agents.Frequency/      # Purchase frequency agent
│   │   │   ├── FrequencyAgent.cs
│   │   │   └── Services/
│   │   │       └── FrequencyCalculator.cs
│   │   ├── AgenticShopper.Agents.ListGenerator/  # Shopping list generator agent
│   │   │   ├── ListGeneratorAgent.cs
│   │   │   └── Services/
│   │   │       └── RecommendationEngine.cs
│   │   ├── AgenticShopper.Agents.PriceComparison/ # Price & promotion agent
│   │   │   ├── PriceAgent.cs
│   │   │   └── Services/
│   │   │       ├── ColesCatalogService.cs
│   │   │       ├── WoolworthsCatalogService.cs
│   │   │       └── PriceOptimizer.cs
│   │   ├── AgenticShopper.Agents.Budget/         # Budget tracking agent
│   │   │   ├── BudgetAgent.cs
│   │   │   └── Services/
│   │   │       ├── SpendingAnalyzer.cs
│   │   │       └── BudgetAlertService.cs
│   │   ├── AgenticShopper.Core/                  # Shared core library
│   │   │   ├── Models/                          # Domain entities
│   │   │   │   ├── Receipt.cs
│   │   │   │   ├── Product.cs
│   │   │   │   ├── Purchase.cs
│   │   │   │   ├── ShoppingList.cs
│   │   │   │   ├── Store.cs
│   │   │   │   ├── Category.cs
│   │   │   │   ├── Budget.cs
│   │   │   │   ├── Promotion.cs
│   │   │   │   ├── UserProfile.cs
│   │   │   │   └── FamilyAccount.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAgentBase.cs
│   │   │   │   ├── ILlmProvider.cs
│   │   │   │   └── IDataRepository.cs
│   │   │   └── Abstractions/
│   │   │       ├── FoundryLlmProvider.cs         # Foundry Local integration
│   │   │       ├── AzureAIFoundryProvider.cs     # Azure AI Foundry integration
│   │   │       └── LlmProviderFactory.cs
│   │   ├── AgenticShopper.Data/                  # Data access layer
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   └── AgenticShopper.API/                   # Main API gateway (optional)
│   │       ├── Program.cs
│   │       ├── Controllers/
│   │       └── Middleware/
│   ├── tests/
│   │   ├── AgenticShopper.Tests.Unit/
│   │   │   ├── Agents/
│   │   │   ├── Services/
│   │   │   └── Models/
│   │   ├── AgenticShopper.Tests.Integration/
│   │   │   ├── AgentOrchestration/
│   │   │   ├── ApiContracts/
│   │   │   └── EndToEnd/
│   │   └── AgenticShopper.Tests.Contract/
│   │       ├── ReceiptAgentContractTests.cs
│   │       └── [agent]-ContractTests.cs
│   └── deployment/
│       ├── docker/
│       │   ├── Dockerfile.coordinator
│       │   ├── Dockerfile.agent
│       │   └── docker-compose.yml
│       ├── kubernetes/
│       │   ├── coordinator-deployment.yaml
│       │   ├── agents-deployment.yaml
│       │   ├── services.yaml
│       │   └── ingress.yaml
│       └── azure/
│           ├── container-apps.bicep
│           └── foundry-agent-service.bicep
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── receipts/
│   │   │   │   ├── ReceiptUpload.tsx
│   │   │   │   ├── ReceiptReview.tsx
│   │   │   │   └── ReceiptList.tsx
│   │   │   ├── products/
│   │   │   │   ├── ProductCategorization.tsx
│   │   │   │   ├── FrequencyAssignment.tsx
│   │   │   │   └── ProductNotes.tsx
│   │   │   ├── shopping-lists/
│   │   │   │   ├── ListGenerator.tsx
│   │   │   │   ├── ListEditor.tsx
│   │   │   │   ├── PriceComparison.tsx
│   │   │   │   └── ListSharing.tsx
│   │   │   ├── analytics/
│   │   │   │   ├── SpendingDashboard.tsx
│   │   │   │   ├── BudgetTracker.tsx
│   │   │   │   └── Charts.tsx
│   │   │   └── common/
│   │   │       ├── Header.tsx
│   │   │       └── Navigation.tsx
│   │   ├── pages/
│   │   │   ├── HomePage.tsx
│   │   │   ├── ReceiptsPage.tsx
│   │   │   ├── ProductsPage.tsx
│   │   │   ├── ShoppingListsPage.tsx
│   │   │   ├── AnalyticsPage.tsx
│   │   │   └── SettingsPage.tsx
│   │   ├── services/
│   │   │   ├── api/
│   │   │   │   ├── receiptApi.ts
│   │   │   │   ├── productApi.ts
│   │   │   │   ├── shoppingListApi.ts
│   │   │   │   ├── priceApi.ts
│   │   │   │   └── budgetApi.ts
│   │   │   └── websocket/
│   │   │       └── realtimeSync.ts
│   │   ├── hooks/
│   │   ├── contexts/
│   │   ├── types/
│   │   └── utils/
│   ├── tests/
│   │   ├── unit/
│   │   ├── integration/
│   │   └── e2e/
│   ├── public/
│   ├── package.json
│   └── tsconfig.json
├── .github/
│   └── workflows/
│       ├── ci-backend.yml
│       ├── ci-frontend.yml
│       └── deploy.yml
└── README.md
```

**Structure Decision**: Web application with clear separation of concerns:
- **Multi-agent backend**: Each agent is self-contained project with its own API
- **Workflow orchestration**: Microsoft Agent Framework WorkflowBuilder for graph-based multi-agent coordination
  - Sequential workflows (e.g., OCR → Parse → Categorize)
  - Conditional routing (e.g., budget alerts only when threshold exceeded)
  - Parallel execution (e.g., simultaneous price comparison across multiple stores)
  - Streaming support for real-time progress updates
- **Shared core**: Common models, interfaces, and LLM abstractions
- **API-first**: Every agent exposes REST APIs (can be consumed independently)
- **React frontend**: Modern SPA consuming backend APIs via service layer
- **Deployment flexibility**: Docker/K8s manifests + Azure-specific configurations

## Complexity Tracking

> **Complexity justifications for architectural decisions**

| Decision | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|-------------------------------------|
| Multi-agent architecture (7 agents) | Autonomous operation, independent scaling, clear responsibility boundaries per spec requirement | Monolithic service would couple unrelated concerns (OCR, categorization, price lookup) preventing independent deployment and scaling |
| Microsoft Agent Framework Workflows | Built-in graph-based orchestration, conditional routing, parallel execution, streaming support, and workflow visualization | Custom orchestration logic would require reimplementing workflow engine features (state management, event streaming, error handling, DAG execution) |
| LLM provider abstraction layer | Must support Foundry Local during dev + Azure AI Foundry in production per requirement | Direct LLM coupling would require code changes for environment migration, violating portability requirement |
| Separate frontend project | React SPA requirement, independent deployment pipeline, different technology stack | Backend-rendered views insufficient for real-time collaborative editing (FR-040) and rich UX requirements |
| PostgreSQL + Blob Storage | Relational data (receipts, products, relationships) + large binary files (receipt images FR-005) | Single storage solution: PostgreSQL alone expensive for images, Blob alone lacks relational query capabilities needed for frequency analysis |
| WorkflowBuilder with typed executors | Type-safe agent coordination, compile-time validation of workflow edges, built-in error handling and event streaming | Manual agent-to-agent HTTP calls create tight coupling, lack type safety, require custom error handling and retry logic |
| Docker + K8s + Azure deployment configs | Support local dev, on-prem K8s, Azure Container Apps, Foundry Agent Service per requirement | Single deployment target violates multi-environment portability requirement explicitly stated |

## Workflow Architecture (Agent Framework)

### Core Workflows

The coordinator uses **Microsoft Agent Framework's WorkflowBuilder** to orchestrate multi-agent interactions as directed acyclic graphs (DAGs):

#### 1. Receipt Processing Workflow
```csharp
// Sequential pipeline with conditional branching
var workflow = new WorkflowBuilder(receiptAgent)
    .AddEdge(receiptAgent, categorizationAgent)
    .AddEdge(categorizationAgent, frequencyAgent)
    .AddEdge(frequencyAgent, completionExecutor)
    .Build();

// Streaming execution for real-time progress updates
await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, receiptUploadMessage);
```

**Flow**: Upload → OCR → Parse → Categorize Products → Calculate Frequencies → Complete

**Features**:
- Streaming updates for UI progress indicators
- Error handling with automatic rollback
- Idempotent execution (can retry failed steps)

#### 2. Shopping List Generation Workflow
```csharp
// Parallel price comparison across stores
var colesExecutor = priceAgent.BindAsExecutor("ColesComparison");
var woolworthsExecutor = priceAgent.BindAsExecutor("WoolworthsComparison");

var workflow = new WorkflowBuilder(frequencyAgent)
    .AddEdge(frequencyAgent, colesExecutor)
    .AddEdge(frequencyAgent, woolworthsExecutor)
    .AddEdge(colesExecutor, listGeneratorAgent)
    .AddEdge(woolworthsExecutor, listGeneratorAgent)
    .Build();
```

**Flow**: Frequency Analysis → [Parallel: Coles Prices | Woolworths Prices] → Optimize List

**Features**:
- Parallel execution reduces latency (2s target)
- Aggregates results from multiple stores
- Optimizes based on best prices + frequency needs

#### 3. Budget Analysis Workflow
```csharp
// Conditional routing based on budget thresholds
var workflow = new WorkflowBuilder(budgetAgent)
    .AddEdge(budgetAgent, alertExecutor, condition: ctx => ctx.IsOverBudget)
    .AddEdge(budgetAgent, summaryExecutor)
    .Build();
```

**Flow**: Spending Analysis → [If Over Budget: Send Alert] → Summary Dashboard

**Features**:
- Conditional execution (alerts only when needed)
- Real-time budget tracking
- Integration with notification services

### Workflow Benefits

| Capability | How Agent Framework Provides It | Implementation Example |
|------------|----------------------------------|------------------------|
| **Type Safety** | Typed executors (`Executor<TIn, TOut>`) with compile-time validation | `ReceiptAgent : Executor<ReceiptUpload, ParsedReceipt>` |
| **Streaming** | `StreamingRun` with `WatchStreamAsync()` for real-time events | `await foreach (var evt in run.WatchStreamAsync())` |
| **Conditional Routing** | `AddEdge(from, to, condition: Func<bool>)` for branching logic | `.AddEdge(agent, alertAgent, condition: ctx => ctx.Total > budget)` |
| **Parallel Execution** | Multiple edges from single executor execute concurrently | `.AddEdge(freq, colesAgent).AddEdge(freq, woolworthsAgent)` |
| **Visualization** | `ToMermaidString()` and `ToDotString()` for documentation | `workflow.ToMermaidString()` generates Mermaid diagrams |
| **Error Handling** | Built-in exception propagation and event logging | Automatic workflow state management on failures |
| **State Management** | `WorkflowContext` tracks execution state across agents | `ctx.GetState<ReceiptState>()` for cross-agent data |

### Implementation Pattern

**Each workflow file** (e.g., `ReceiptProcessingWorkflow.cs`) will:
1. Define typed executors wrapping agent APIs
2. Build workflow graph using WorkflowBuilder
3. Expose `ExecuteAsync()` and `ExecuteStreamingAsync()` methods
4. Provide `GetVisualization()` for documentation/debugging

**Example**:
```csharp
public class ReceiptProcessingWorkflow
{
    private readonly Workflow _workflow;
    
    public ReceiptProcessingWorkflow(
        ReceiptAgent receiptAgent,
        CategorizationAgent categorizationAgent,
        FrequencyAgent frequencyAgent)
    {
        var builder = new WorkflowBuilder(receiptAgent);
        builder.AddEdge(receiptAgent, categorizationAgent)
               .AddEdge(categorizationAgent, frequencyAgent)
               .WithOutputFrom(frequencyAgent);
        _workflow = builder.Build();
    }
    
    public async Task<ReceiptProcessingResult> ExecuteAsync(ReceiptUpload upload)
    {
        await using Run run = await InProcessExecution.RunAsync(_workflow, upload);
        return run.GetOutputs<ReceiptProcessingResult>().First();
    }
    
    public string GetVisualization() => _workflow.ToMermaidString();
}
```

This approach eliminates custom orchestration code while providing enterprise-grade workflow capabilities.
