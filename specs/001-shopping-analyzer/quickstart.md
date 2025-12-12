# Developer Quickstart Guide

**Feature**: Smart Shopping Pattern Analyzer & Recommender  
**Last Updated**: December 13, 2025

## Prerequisites

- **.NET 8.0 SDK** or later
- **Node.js 18+** and npm
- **Docker Desktop** (for local development)
- **PostgreSQL 15+** (or use Docker)
- **Git**
- **VS Code** or **Visual Studio 2022**

Optional:
- **Postman** or **Insomnia** (API testing)
- **Azure CLI** (for Azure deployments)
- **kubectl** (for Kubernetes deployments)

---

## Quick Start (5 Minutes)

### 1. Clone Repository

```bash
git clone https://github.com/NileshGule/agentic-shopper.git
cd agentic-shopper
```

### 2. Start Local Environment with Docker Compose

```bash
cd backend/deployment/docker
docker-compose up -d
```

This starts:
- PostgreSQL database
- Foundry Local (LLM server)
- All agent services
- Coordinator service
- Frontend dev server

### 3. Verify Services

```bash
# Check all containers running
docker-compose ps

# Should see:
# - coordinator (port 5000)
# - receipt-agent (port 5001)
# - categorization-agent (port 5002)
# - frequency-agent (port 5003)
# - list-generator-agent (port 5004)
# - price-agent (port 5005)
# - budget-agent (port 5006)
# - postgres (port 5432)
# - foundry-local (port 8080)
# - frontend (port 3000)
```

### 4. Access Applications

- **Frontend**: http://localhost:3000
- **API Swagger**: http://localhost:5000/swagger
- **Coordinator API**: http://localhost:5000/v1
- **Foundry Local UI**: http://localhost:8080

### 5. Run Sample Workflow

```bash
# Upload a test receipt
curl -X POST http://localhost:5000/v1/coordinator/receipts/process \
  -H "Authorization: Bearer {token}" \
  -F "file=@tests/sample-receipts/coles-receipt-1.jpg" \
  -F "familyId=00000000-0000-0000-0000-000000000001" \
  -F "uploadedBy=00000000-0000-0000-0000-000000000002"

# Response: { "receiptId": "...", "workflowId": "..." }

# Check processing status
curl http://localhost:5000/v1/coordinator/receipts/{receiptId}/status
```

---

## Development Setup

### Backend (.NET)

#### 1. Install Dependencies

```bash
cd backend
dotnet restore
```

#### 2. Configure Settings

Create `appsettings.Development.json` in each agent project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=agentic_shopper;Username=postgres;Password=postgres"
  },
  "LLM": {
    "Provider": "FoundryLocal",
    "Endpoint": "http://localhost:8080",
    "Model": "phi-3-mini"
  },
  "OCR": {
    "Provider": "Local",  // Use PaddleOCR for local dev
    "Endpoint": "http://localhost:9000"
  },
  "BlobStorage": {
    "Provider": "Local",
    "Path": "./uploads/receipts"
  }
}
```

#### 3. Run Database Migrations

```bash
cd src/AgenticShopper.Data
dotnet ef migrations add InitialCreate
dotnet ef database update
```

#### 4. Run Individual Agent

```bash
# Example: Run coordinator
cd src/AgenticShopper.Coordinator
dotnet run

# Or run all agents in separate terminals
dotnet run --project src/AgenticShopper.Coordinator
dotnet run --project src/AgenticShopper.Agents.Receipt
# ... etc
```

#### 5. Run Tests

```bash
# Unit tests
dotnet test tests/AgenticShopper.Tests.Unit

# Integration tests (requires Docker)
dotnet test tests/AgenticShopper.Tests.Integration

# Contract tests
dotnet test tests/AgenticShopper.Tests.Contract
```

---

### Frontend (React)

#### 1. Install Dependencies

```bash
cd frontend
npm install
```

#### 2. Configure Environment

Create `.env.development`:

```env
REACT_APP_API_URL=http://localhost:5000/v1
REACT_APP_SIGNALR_URL=http://localhost:5000/hubs/shopping-list
REACT_APP_AUTH_ENABLED=false  # For local dev
```

#### 3. Start Development Server

```bash
npm start
# Opens http://localhost:3000
```

#### 4. Run Tests

```bash
# Unit tests
npm test

# E2E tests (requires backend running)
npm run test:e2e

# Coverage
npm run test:coverage
```

---

## Project Structure Navigation

### Backend Projects

```
backend/src/
├── AgenticShopper.Coordinator/       # Main orchestrator
│   ├── Controllers/                  # API endpoints
│   ├── Services/                     # Business logic
│   └── Program.cs                    # Startup
│
├── AgenticShopper.Agents.*/          # Individual agents
│   ├── {Agent}Agent.cs               # Agent implementation
│   ├── Services/                     # Agent-specific logic
│   └── Prompts/                      # LLM prompts
│
├── AgenticShopper.Core/              # Shared library
│   ├── Models/                       # Domain entities
│   ├── Interfaces/                   # Abstractions
│   └── Abstractions/                 # LLM providers
│
└── AgenticShopper.Data/              # EF Core DbContext
    ├── ApplicationDbContext.cs
    ├── Repositories/
    └── Migrations/
```

### Frontend Structure

```
frontend/src/
├── components/                       # React components
│   ├── receipts/                    # Receipt management
│   ├── products/                    # Product management
│   ├── shopping-lists/              # List features
│   └── analytics/                   # Dashboards
│
├── pages/                           # Route pages
├── services/api/                    # API clients
├── hooks/                           # Custom React hooks
└── types/                           # TypeScript types
```

---

## Common Development Tasks

### Add a New Agent

1. **Create Project**:
   ```bash
   dotnet new webapi -n AgenticShopper.Agents.NewAgent
   cd src
   dotnet sln add AgenticShopper.Agents.NewAgent
   ```

2. **Implement IAgent Interface**:
   ```csharp
   public class NewAgent : AgentBase, IAgent
   {
       public override async Task<AgentResponse> ExecuteAsync(AgentRequest request)
       {
           // Agent logic
       }
   }
   ```

3. **Add API Controller**:
   ```csharp
   [ApiController]
   [Route("api/v1/new-agent")]
   public class NewAgentController : ControllerBase
   {
       private readonly IAgent _agent;
       
       [HttpPost]
       public async Task<IActionResult> Execute([FromBody] Request request)
       {
           var result = await _agent.ExecuteAsync(request);
           return Ok(result);
       }
   }
   ```

4. **Register in Coordinator**:
   ```csharp
   services.AddHttpClient<INewAgent, NewAgentClient>(client =>
   {
       client.BaseAddress = new Uri(config["Agents:NewAgent:Url"]);
   });
   ```

5. **Add Docker Configuration**:
   Update `docker-compose.yml` with new service

6. **Create OpenAPI Spec**:
   Add `new-agent-api.yaml` to `/specs/001-shopping-analyzer/contracts/`

---

### Add a New Frontend Component

1. **Create Component**:
   ```bash
   cd frontend/src/components/new-feature
   touch NewComponent.tsx NewComponent.test.tsx NewComponent.module.css
   ```

2. **Implement Component**:
   ```typescript
   import React from 'react';
   import styles from './NewComponent.module.css';
   
   export const NewComponent: React.FC = () => {
       return <div className={styles.container}>...</div>;
   };
   ```

3. **Add API Service**:
   ```typescript
   // services/api/newApi.ts
   export const newApi = {
       async fetchData(): Promise<Data> {
           const response = await fetch(`${API_URL}/new-endpoint`);
           return response.json();
       }
   };
   ```

4. **Write Tests**:
   ```typescript
   import { render, screen } from '@testing-library/react';
   import { NewComponent } from './NewComponent';
   
   test('renders component', () => {
       render(<NewComponent />);
       expect(screen.getByText(/expected text/i)).toBeInTheDocument();
   });
   ```

---

### Run Specific User Story Tests

Tests are organized by user story priority:

```bash
# P1: Receipt capture and categorization
dotnet test --filter "Category=P1"

# P2: List generation and price comparison
dotnet test --filter "Category=P2"

# P3: Analytics and budget tracking
dotnet test --filter "Category=P3"
```

---

## Debugging

### Backend (Visual Studio Code)

`.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Coordinator",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/backend/src/AgenticShopper.Coordinator/bin/Debug/net8.0/AgenticShopper.Coordinator.dll",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Frontend (Chrome DevTools)

1. Start dev server: `npm start`
2. Open Chrome DevTools (F12)
3. Set breakpoints in Sources tab
4. Or use `debugger;` statement in code

### Docker Logs

```bash
# View logs for specific service
docker-compose logs -f coordinator

# View all logs
docker-compose logs -f

# Follow logs with timestamps
docker-compose logs -f --timestamps
```

---

## Testing Strategies

### Unit Tests (Fast, Isolated)

```csharp
[Fact]
public void FrequencyCalculator_WithThreePurchases_ReturnsWeekly()
{
    // Arrange
    var calculator = new FrequencyCalculator();
    var dates = new List<DateTime> { 
        new(2025, 1, 1), 
        new(2025, 1, 8), 
        new(2025, 1, 15) 
    };
    
    // Act
    var frequency = calculator.CalculateFrequency(dates);
    
    // Assert
    Assert.Equal(PurchaseFrequency.Weekly, frequency);
}
```

### Integration Tests (Database, External Services)

```csharp
[Fact]
public async Task ReceiptAgent_ProcessReceipt_StoresInDatabase()
{
    // Arrange
    await using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.PostAsync("/api/v1/receipt/process", ...);
    
    // Assert
    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    // Verify database record created
}
```

### Contract Tests (API Schemas)

```csharp
[Fact]
public async Task CoordinatorAPI_MatchesOpenAPISpec()
{
    var spec = await File.ReadAllTextAsync("contracts/coordinator-api.yaml");
    var validator = new OpenApiValidator(spec);
    
    var response = await _client.PostAsync("/v1/coordinator/receipts/process", ...);
    
    validator.ValidateResponse(response); // Throws if schema mismatch
}
```

---

## Deployment

### Local (Docker Compose)

Already covered in Quick Start section above.

### Kubernetes

```bash
# Build images
docker build -t agenticshopper/coordinator:latest -f deployment/docker/Dockerfile.coordinator .
docker build -t agenticshopper/agent:latest -f deployment/docker/Dockerfile.agent .

# Push to registry
docker push agenticshopper/coordinator:latest
docker push agenticshopper/agent:latest

# Deploy to K8s
kubectl apply -f deployment/kubernetes/
kubectl get pods  # Verify pods running
```

### Azure Container Apps

```bash
# Login to Azure
az login

# Deploy using Bicep
cd deployment/azure
az deployment group create \
  --resource-group agentic-shopper-rg \
  --template-file container-apps.bicep \
  --parameters @parameters.json

# Get app URL
az containerapp show \
  --name coordinator \
  --resource-group agentic-shopper-rg \
  --query properties.configuration.ingress.fqdn
```

---

## Troubleshooting

### OCR Not Working

**Problem**: Receipt upload fails with OCR error  
**Solution**:
1. Check Foundry Local is running: `docker ps | grep foundry-local`
2. Verify model downloaded: `docker exec foundry-local ls /models`
3. Check OCR service logs: `docker-compose logs ocr-service`

### Database Connection Errors

**Problem**: `Npgsql.NpgsqlException: connection refused`  
**Solution**:
1. Verify PostgreSQL running: `docker ps | grep postgres`
2. Check connection string in `appsettings.Development.json`
3. Ensure database created: `docker exec postgres psql -U postgres -l`

### Frontend API Calls Failing

**Problem**: `ERR_CONNECTION_REFUSED` in browser console  
**Solution**:
1. Verify backend running: `curl http://localhost:5000/health`
2. Check CORS settings in backend `Program.cs`
3. Verify `REACT_APP_API_URL` in `.env.development`

### SignalR Not Connecting

**Problem**: Real-time list updates not working  
**Solution**:
1. Check SignalR hub endpoint: `http://localhost:5000/hubs/shopping-list`
2. Verify WebSocket support enabled in browser
3. Check firewall/proxy settings
4. Fall back to long polling in `frontend/src/services/websocket/realtimeSync.ts`

---

## Useful Commands

```bash
# Backend
dotnet build                          # Build all projects
dotnet test                           # Run all tests
dotnet ef migrations add <name>       # Create migration
dotnet ef database update             # Apply migrations
dotnet format                         # Format code

# Frontend
npm run build                         # Production build
npm run lint                          # Lint code
npm run format                        # Format with Prettier
npm run analyze                       # Bundle size analysis

# Docker
docker-compose up -d                  # Start all services
docker-compose down                   # Stop all services
docker-compose restart <service>      # Restart specific service
docker-compose logs -f <service>      # View service logs
docker system prune -a                # Clean up Docker (careful!)

# Kubernetes
kubectl get pods                      # List pods
kubectl logs <pod-name>               # View pod logs
kubectl describe pod <pod-name>       # Pod details
kubectl exec -it <pod-name> -- bash   # Shell into pod
kubectl port-forward <pod> 5000:80    # Local port forward
```

---

## Next Steps

1. **Read Specification**: Review `spec.md` for full requirements
2. **Explore API Contracts**: Check `contracts/` directory for API specs
3. **Run Sample Workflows**: Use Postman collection in `/tests/postman/`
4. **Contribute**: See `CONTRIBUTING.md` for guidelines

## Getting Help

- **Documentation**: `/docs` directory
- **API Reference**: http://localhost:5000/swagger (when running)
- **Issues**: https://github.com/NileshGule/agentic-shopper/issues

---

**Happy Coding! 🛒🤖**
