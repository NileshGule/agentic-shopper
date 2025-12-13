# 🚀 Quick Reference: Local Development vs Azure

## Start Local Development

```powershell
# Windows
.\start-dev.ps1

# Linux/macOS
./start-dev.sh
```

## Service Comparison

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

### Start Services
```bash
docker-compose -f docker-compose.dev.yml up -d
```

### Stop Services
```bash
docker-compose -f docker-compose.dev.yml down
```

### View Logs
```bash
docker-compose -f docker-compose.dev.yml logs -f [service-name]
```

### Check Status
```bash
docker-compose -f docker-compose.dev.yml ps
```

## Access Local Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **RabbitMQ UI** | http://localhost:15672 | admin / admin |
| **PostgreSQL** | localhost:5432 | postgres / postgres |
| **Azurite Blob** | http://localhost:10000 | (well-known dev key) |
| **Redis** | localhost:6379 | (no auth) |
| **Kafka** | localhost:9092 | (no auth) |

## Health Checks

```bash
# PostgreSQL
docker exec agentic-shopper-postgres pg_isready -U postgres

# Redis
docker exec agentic-shopper-redis redis-cli PING

# RabbitMQ
docker exec agentic-shopper-rabbitmq rabbitmq-diagnostics ping
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
| Port already in use | Stop conflicting service or change port in `docker-compose.dev.yml` |
| Container won't start | Check logs: `docker logs [container-name]` |
| Database connection refused | Wait 10s for health check, or restart: `docker restart agentic-shopper-postgres` |
| Azurite not accessible | Verify port 10000 is free: `netstat -an \| grep 10000` |

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
