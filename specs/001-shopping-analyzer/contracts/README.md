# API Contracts Overview

**Feature**: Smart Shopping Pattern Analyzer & Recommender  
**Date**: December 13, 2025  
**API Version**: v1

## Agent APIs

This directory contains OpenAPI 3.0 specifications for all autonomous agents in the system.

### Agent API Endpoints

| Agent | Base Path | Purpose | Key Operations |
|-------|-----------|---------|----------------|
| **Receipt Agent** | `/api/v1/receipt` | OCR processing, receipt extraction | Upload, review, verify |
| **Categorization Agent** | `/api/v1/categorization` | Product category classification | Suggest category, assign, customize |
| **Frequency Agent** | `/api/v1/frequency` | Purchase frequency analysis | Calculate, update, pause |
| **List Generator Agent** | `/api/v1/list-generator` | Shopping list recommendations | Generate, suggest items |
| **Price Comparison Agent** | `/api/v1/price` | Promotions & price comparison | Get promotions, compare stores |
| **Budget Agent** | `/api/v1/budget` | Spending analytics & alerts | Track spending, set budgets, alert |
| **Coordinator** | `/api/v1/coordinator` | Workflow orchestration | Orchestrate workflows |

### API Files

- `coordinator-api.yaml` - Coordinator orchestration API
- `receipt-agent-api.yaml` - Receipt processing agent API
- `categorization-agent-api.yaml` - Categorization agent API  
- `frequency-agent-api.yaml` - Frequency analysis agent API
- `list-generator-agent-api.yaml` - Shopping list generation API
- `price-agent-api.yaml` - Price & promotion API
- `budget-agent-api.yaml` - Budget tracking API

### Common Patterns

#### Authentication
All APIs use Bearer token authentication (JWT):
```yaml
security:
  - bearerAuth: []
```

#### Error Responses
Standard error format across all agents:
```json
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "Receipt image exceeds 10MB limit",
    "details": {
      "field": "file",
      "constraint": "maxSize",
      "limit": "10MB"
    }
  }
}
```

#### Pagination
List operations support pagination:
```
GET /api/v1/receipts?page=1&pageSize=20
```

Response includes pagination metadata:
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalPages": 5,
    "totalItems": 97
  }
}
```

### Agent Communication Flow

```
┌────────────┐
│  Frontend  │
└─────┬──────┘
      │
      │ HTTP/REST
      ▼
┌─────────────────┐
│  Coordinator    │◄──────┐
│     Agent       │       │
└────────┬────────┘       │
         │                │
         │ Internal       │
         │ HTTP calls     │ Async
         ▼                │ Events
  ┌──────────────┐        │
  │Receipt Agent │        │
  └──────┬───────┘        │
         │                │
         ▼                │
  ┌─────────────────┐     │
  │Categorization   │     │
  │     Agent       │     │
  └──────┬──────────┘     │
         │                │
         ▼                │
  ┌──────────────┐        │
  │Frequency     │        │
  │   Agent      │        │
  └──────────────┘        │
                          │
  ┌──────────────┐        │
  │Price Agent   │────────┘
  │(Background)  │ Weekly
  └──────────────┘ Refresh
```

### Quick Reference

#### Upload Receipt Workflow
```
POST /api/v1/coordinator/receipts/process
  → Uploads image
  → Triggers OCR (Receipt Agent)
  → Auto-categorizes products (Categorization Agent)
  → Calculates frequencies (Frequency Agent)
  → Returns structured receipt data
```

#### Generate Shopping List Workflow
```
POST /api/v1/coordinator/shopping-lists/generate
  → Analyzes purchase history (Frequency Agent)
  → Identifies due products
  → Checks promotions (Price Agent)
  → Creates list with price comparisons
  → Returns optimized shopping list
```

#### Budget Alert Flow
```
Event: New purchase recorded
  → Budget Agent recalculates spending
  → Checks threshold (90% by default)
  → Sends alert if exceeded
  → Updates budget status
```

### Testing

Each agent API includes:
- Contract tests (validate request/response schemas)
- Integration tests (end-to-end workflows)
- Performance tests (response time, concurrency)

### Documentation Generation

Generate interactive API docs from OpenAPI specs:

```bash
# Install Redoc CLI
npm install -g redoc-cli

# Generate HTML docs for all agents
redoc-cli bundle coordinator-api.yaml -o docs/coordinator-api.html
redoc-cli bundle receipt-agent-api.yaml -o docs/receipt-api.html
# ... repeat for all agents
```

### Next Steps

1. Implement each agent following the API contracts
2. Set up Swagger UI for interactive testing
3. Create Postman/Insomnia collections for manual testing
4. Write contract tests using xUnit + OpenAPI validator

**See individual YAML files for detailed specifications of each agent API.**
