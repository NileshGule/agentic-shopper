# 🚀 Quick Reference: Local Development

## Start Everything (Docker Compose)

```bash
# Recommended: Start full stack (Frontend + Backend + Infrastructure)
docker compose -f docker-compose.local.yml up --build -d

# Check status
docker compose -f docker-compose.local.yml ps

# View logs
docker compose -f docker-compose.local.yml logs -f coordinator
```

## Start Infrastructure Only (for local dev)

```powershell
# Windows
.\start-dev.ps1

# Linux/macOS
./start-dev.sh

# Or manually
docker compose -f docker-compose.dev.yml up -d
```

## Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | - |
| **API (Coordinator)** | http://localhost:5050 | - |
| **Swagger UI** | http://localhost:5050 | - |
| **Health Check** | http://localhost:5050/health | - |
| **PostgreSQL** | localhost:5432 | postgres / postgres |
| **Azurite Blob** | http://localhost:10000 | (well-known dev key) |
| **Redis** | localhost:6379 | (no auth) |

> **macOS Note**: Port 5000 is used by AirPlay Receiver. The coordinator uses port **5050**.

## Demo Accounts (Auto-Seeded)

The backend automatically creates demo data on first startup:

| Entity | GUID |
|--------|------|
| Demo Family | `00000000-0000-0000-0000-000000000001` |
| Demo User | `00000000-0000-0000-0000-000000000001` |

Frontend uses these IDs automatically via `frontend/src/constants/demo.ts`.

### Quick Test — Upload a Receipt
```bash
curl -X POST http://localhost:5050/api/receipts/upload \
  -F "file=@path/to/receipt.jpg" \
  -F "familyId=00000000-0000-0000-0000-000000000001" \
  -F "uploadedBy=00000000-0000-0000-0000-000000000001"
```

### Verify DB Data
```bash
docker exec agentic-shopper-postgres psql -U postgres -d agentic_shopper \
  -c 'SELECT "Id","StoreName","TotalAmount" FROM "Receipts";'
```

## Service Comparison (Local vs Azure)

| Service | Local (Free) | Azure (Paid) | Monthly Savings |
|---------|--------------|--------------|----------------|
| **Blob Storage** | Azurite `localhost:10000` | Azure Storage | $20-50 |
| **Database** | PostgreSQL `localhost:5432` | Azure Database | $50-200 |
| **Cache** | Redis `localhost:6379` | Azure Cache | $15-100 |
| **Message Queue** | RabbitMQ `localhost:5672` | Service Bus | $10-50 |
| **Event Stream** | Kafka `localhost:9092` | Event Hubs | $20-100 |
| **OCR** | PaddleOCR (CLI) | Document Intelligence | $1-10/1000 |
| **TOTAL** | **$0** | **$116-510** | **$116-510** 💰 |

## Configuration Files

### Development (Local)
**File:** `appsettings.Development.json`
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;...",
    "UseAzurite": true
  },
  "OCR": {
    "PreferredProvider": "PaddleOCR"
  }
}
```

### Production (Azure)
**File:** `appsettings.Production.json`
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstore;AccountKey=...",
    "UseAzurite": false
  },
  "OCR": {
    "PreferredProvider": "AzureDocumentIntelligence"
  }
}
```

## Common Commands

### Start All Services (Full Stack)
```bash
docker compose -f docker-compose.local.yml up --build -d
```

### Start Infrastructure Only
```bash
docker compose -f docker-compose.dev.yml up -d
```

### Stop Services
```bash
docker compose -f docker-compose.local.yml down
```

### View Logs
```bash
docker compose -f docker-compose.local.yml logs -f [service-name]
```

### Check Status
```bash
docker compose -f docker-compose.local.yml ps
```

### Rebuild a Single Service
```bash
docker compose -f docker-compose.local.yml up --build coordinator -d
```

## Health Checks

```bash
# Application health
curl http://localhost:5050/health

# PostgreSQL
docker exec agentic-shopper-postgres pg_isready -U postgres

# Redis
docker exec agentic-shopper-redis redis-cli PING
```

## Environment Variables

```bash
# Development
$env:ASPNETCORE_ENVIRONMENT="Development"  # Windows
export ASPNETCORE_ENVIRONMENT=Development   # Linux/macOS

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_ENVIRONMENT=Production
```

## Quick Troubleshooting

| Issue | Solution |
|-------|----------|
| Port 5000 in use (macOS) | Coordinator uses port **5050** — macOS AirPlay uses 5000 |
| Port already in use | Stop conflicting service or change port in `docker-compose.local.yml` |
| Container won't start | Check logs: `docker compose -f docker-compose.local.yml logs coordinator` |
| Coordinator crash loop | Check DI errors: `docker compose -f docker-compose.local.yml logs coordinator --tail 50` |
| Database connection refused | Wait 10s for health check, or restart: `docker restart agentic-shopper-postgres` |
| Frontend "Network Error" | Verify coordinator is healthy: `curl http://localhost:5050/health` |
| Upload returns 400 | Ensure `familyId` and `uploadedBy` are valid GUIDs (use demo GUID above) |
| Upload returns 500 FK error | Reset DB: `docker compose -f docker-compose.local.yml down -v` then rebuild |
| DB schema out of date | Reset: `docker compose -f docker-compose.local.yml down -v && docker compose -f docker-compose.local.yml up --build -d` |
| ObjectDisposedException | Pull latest code — fixed via `IServiceScopeFactory` in background tasks |

## Migration to Azure

1. Update `appsettings.Production.json` with Azure connection strings
2. Deploy infrastructure (Bicep/Terraform)
3. Run migrations on Azure PostgreSQL
4. Upload blobs from Azurite to Azure Storage
5. Configure Service Bus queues/topics
6. Enable Document Intelligence API

**No code changes required!** The application works with both environments seamlessly. 🎉

---

📖 **Full Documentation:** [DEVELOPMENT.md](DEVELOPMENT.md)
