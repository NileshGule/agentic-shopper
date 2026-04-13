# Local Development Guide

This guide explains how to run Agentic Shopper locally using Docker containers instead of Azure cloud services to save costs during development.

## 🚀 Quick Start

### Prerequisites
- Docker Desktop installed and running
- .NET 10 SDK (for local development without Docker)
- Node.js 20+ (for frontend development without Docker)

### Start Everything with Docker Compose (Recommended)

The easiest way to get the full application running locally:

```bash
# Build and start all services
docker compose -f docker-compose.local.yml up --build -d

# Verify all services are running and healthy
docker compose -f docker-compose.local.yml ps

# View logs
docker compose -f docker-compose.local.yml logs -f

# View specific service logs
docker compose -f docker-compose.local.yml logs -f coordinator
```

> **Auto-setup on first start**: The coordinator automatically creates the database schema, seeds predefined categories/stores, and creates a demo family + user for testing. No manual migration or seeding is required.

Services available at:
| Service | URL | Purpose |
|---------|-----|---------|
| **Frontend** | http://localhost:3000 | React UI served by nginx |
| **Coordinator API** | http://localhost:5050 | .NET backend API |
| **Health Check** | http://localhost:5050/health | Backend health status |
| **PostgreSQL** | localhost:5432 | Database |
| **Redis** | localhost:6379 | Caching |
| **Azurite** | localhost:10000-10002 | Azure Blob Storage emulator |

> **macOS Note**: Port 5000 is reserved by AirPlay Receiver. The coordinator uses port **5050** instead.

### Start Infrastructure Only (for local .NET/Node dev)

```bash
# Start only infrastructure services
docker compose -f docker-compose.dev.yml up -d

# Verify all services are running
docker compose -f docker-compose.dev.yml ps

# View logs
docker compose -f docker-compose.dev.yml logs -f
```

### Stop Services

```bash
# Stop all services
docker compose -f docker-compose.local.yml down

# Stop and remove volumes (clean slate)
docker compose -f docker-compose.local.yml down -v
```

---

## 📦 Local Services Overview

| Service | Purpose | Local Port | Azure Equivalent | Cost Savings |
|---------|---------|------------|------------------|--------------|
| **Azurite** | Blob/Queue/Table Storage | 10000-10002 | Azure Storage | ~$20-50/month |
| **PostgreSQL** | Database | 5432 | Azure Database for PostgreSQL | ~$50-200/month |
| **Redis** | Caching | 6379 | Azure Cache for Redis | ~$15-100/month |
| **RabbitMQ** | Message Queue | 5672, 15672 | Azure Service Bus | ~$10-50/month |
| **Kafka** | Event Streaming | 9092 | Azure Event Hubs | ~$20-100/month |
| **PaddleOCR** | Receipt OCR | Local CLI | Azure Document Intelligence | ~$1-10/1000 pages |

**Total Estimated Monthly Savings: $116-510** 💰

---

## 🔧 Service Configuration

### 1. Azurite (Azure Storage Emulator)

**Connection String** (in `appsettings.Development.json`):
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;",
    "ContainerName": "receipts",
    "UseAzurite": true
  }
}
```

**Access Azurite**:
- Blob Service: `http://localhost:10000`
- Queue Service: `http://localhost:10001`
- Table Service: `http://localhost:10002`

**Browse Blobs** (using Azure Storage Explorer):
1. Download [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)
2. Connect to local emulator (default connection)
3. Navigate to Blob Containers → `receipts`

### 2. PostgreSQL Database

**Connection String**:
```
Host=localhost;Database=agentic_shopper;Username=postgres;Password=postgres;Port=5432
```

**Connect with psql**:
```bash
docker exec -it agentic-shopper-postgres psql -U postgres -d agentic_shopper
```

**Database Schema**:

The schema is auto-created on startup via `EnsureCreated()` — no manual migration is required. To reset the database, stop the coordinator, drop all tables (or remove the volume), and restart:

```bash
# Full reset (drops all data)
docker compose -f docker-compose.local.yml down -v
docker compose -f docker-compose.local.yml up --build -d
```

**Demo Data (Auto-Seeded)**:

On startup, the coordinator seeds:
- **11 predefined categories**: Dairy, Fresh Produce, Meat & Seafood, Bakery, Pantry & Groceries, Frozen Foods, Beverages, Household & Cleaning, Personal Care, Pet Supplies, Other
- **2 stores**: Coles, Woolworths
- **Demo family**: `00000000-0000-0000-0000-000000000001` ("Demo Family")
- **Demo user**: `00000000-0000-0000-0000-000000000001` ("Demo User" / demo@example.com)

**Verify seeded data**:
```bash
docker exec agentic-shopper-postgres psql -U postgres -d agentic_shopper \
  -c 'SELECT "Id","Name" FROM "FamilyAccounts"; SELECT "Id","Name","Email" FROM "UserProfiles";'
```

### 3. Redis Cache

**Connection String**:
```
localhost:6379
```

**Connect with redis-cli**:
```bash
docker exec -it agentic-shopper-redis redis-cli

# Test connection
> PING
PONG

# View all keys
> KEYS *

# Get value
> GET AgenticShopper:SomeKey
```

### 4. RabbitMQ (Service Bus Alternative)

**Management UI**: http://localhost:15672
- Username: `admin`
- Password: `admin`

**AMQP Connection**: `amqp://admin:admin@localhost:5672`

**Configuration**:
```json
{
  "ServiceBus": {
    "UseLocal": true,
    "LocalEndpoint": "amqp://localhost:5672",
    "ConnectionString": ""
  }
}
```

**Use Cases**:
- Budget alert notifications
- Receipt processing queue
- Agent communication

### 5. Kafka (Event Hub Alternative)

**Bootstrap Server**: `localhost:9092`

**Configuration**:
```json
{
  "EventHub": {
    "UseLocal": true,
    "LocalEndpoint": "localhost:9092",
    "ConnectionString": ""
  }
}
```

**Use Cases**:
- Real-time analytics streaming
- Purchase event logging
- Price change notifications

### 6. PaddleOCR (Document Intelligence Alternative)

**Configuration**:
```json
{
  "OCR": {
    "PreferredProvider": "PaddleOCR"
  },
  "AzureDocumentIntelligence": {
    "Endpoint": "",
    "ApiKey": "",
    "UsePaddleOCR": true
  },
  "PaddleOCR": {
    "ExecutablePath": "/usr/local/bin/paddleocr",
    "Enabled": true
  }
}
```

**Install PaddleOCR** (if not using Docker):

**macOS/Linux**:
```bash
pip install paddleocr
```

**Windows**:
```powershell
pip install paddleocr
```

**Verify Installation**:
```bash
paddleocr --image_dir /path/to/receipt.jpg
```

---

## 🔄 Switching Between Local and Azure

### Development (Local)
Set `ASPNETCORE_ENVIRONMENT=Development`:

```bash
# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Development

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT="Development"

# Windows CMD
set ASPNETCORE_ENVIRONMENT=Development
```

### Staging/Production (Azure)
Set `ASPNETCORE_ENVIRONMENT=Staging` or `Production`:

```bash
export ASPNETCORE_ENVIRONMENT=Production
```

Update `appsettings.Production.json` with Azure connection strings.

---

## 🧪 Testing Local Services

### Test Azurite Blob Storage

```bash
# Upload a test file
curl -X PUT "http://localhost:10000/devstoreaccount1/receipts/test.txt" \
  -H "x-ms-blob-type: BlockBlob" \
  -H "Content-Type: text/plain" \
  --data "Test blob content"

# List blobs (requires Azure Storage SDK or curl with authentication)
```

### Test PostgreSQL

```bash
# Create test data
docker exec -it agentic-shopper-postgres psql -U postgres -d agentic_shopper -c "SELECT version();"
```

### Test Redis

```bash
# Set and get a value
docker exec -it agentic-shopper-redis redis-cli SET test:key "Hello Redis"
docker exec -it agentic-shopper-redis redis-cli GET test:key
```

### Test RabbitMQ

Visit http://localhost:15672 and create a queue named `receipt-processing`.

### Test Kafka

```bash
# Create topic
docker exec -it agentic-shopper-kafka kafka-topics --create \
  --bootstrap-server localhost:9092 \
  --replication-factor 1 \
  --partitions 1 \
  --topic purchase-events

# List topics
docker exec -it agentic-shopper-kafka kafka-topics --list \
  --bootstrap-server localhost:9092
```

---

## 🐛 Troubleshooting

### Azurite won't start
```bash
# Check if port 10000 is already in use
netstat -an | grep 10000

# Stop conflicting process or change port in docker-compose.dev.yml
```

### PostgreSQL connection refused
```bash
# Check if container is running
docker ps | grep postgres

# Check logs
docker logs agentic-shopper-postgres

# Restart container
docker restart agentic-shopper-postgres
```

### Receipt upload returns 500 — FK constraint violation
If you see `FK_Receipts_UserProfiles_UploadedBy` in the logs, the demo user hasn't been seeded. The coordinator seeds demo data automatically on startup. To fix:
```bash
# Reset and rebuild (this re-creates the schema and seeds demo data)
docker compose -f docker-compose.local.yml down -v
docker compose -f docker-compose.local.yml up --build -d
```

### Receipt upload returns 400
Ensure you're sending valid GUIDs for `familyId` and `uploadedBy`. The demo GUID is:
```
00000000-0000-0000-0000-000000000001
```

### ObjectDisposedException in background tasks
This was fixed by using `IServiceScopeFactory` in the controller to create a new DI scope for background work (frequency recalculation). If you see this error, ensure you're running the latest code.

### PaddleOCR not found
```bash
# Verify installation
which paddleocr

# Reinstall
pip uninstall paddleocr -y
pip install paddleocr --upgrade
```

### Redis connection timeout
```bash
# Check if Redis is accepting connections
docker exec -it agentic-shopper-redis redis-cli PING

# If no response, restart
docker restart agentic-shopper-redis
```

---

## 📊 Monitoring Local Services

### View All Container Logs
```bash
docker compose -f docker-compose.local.yml logs -f
```

### View Specific Service Logs
```bash
docker compose -f docker-compose.local.yml logs -f coordinator
docker compose -f docker-compose.local.yml logs -f postgres
docker compose -f docker-compose.local.yml logs -f redis
```

### Check Resource Usage
```bash
docker stats
```

### Health Checks
```bash
# PostgreSQL
docker exec agentic-shopper-postgres pg_isready -U postgres

# Redis
docker exec agentic-shopper-redis redis-cli PING

# RabbitMQ
docker exec agentic-shopper-rabbitmq rabbitmq-diagnostics ping
```

---

## 🔐 Security Notes

⚠️ **The default credentials in `docker-compose.dev.yml` are for local development only!**

- PostgreSQL: `postgres/postgres`
- RabbitMQ: `admin/admin`
- Azurite: Uses well-known development account key

**Never use these credentials in production!**

---

## 🚀 Full Development Workflow

### Option A: Full Stack via Docker (Recommended)

```bash
# 1. Start everything
docker compose -f docker-compose.local.yml up --build -d

# 2. Verify health
curl http://localhost:5050/health

# 3. Test receipt upload
curl -X POST http://localhost:5050/api/receipts/upload \
  -F "file=@path/to/receipt.jpg" \
  -F "familyId=00000000-0000-0000-0000-000000000001" \
  -F "uploadedBy=00000000-0000-0000-0000-000000000001"

# 4. Open the frontend
open http://localhost:3000   # macOS
# xdg-open http://localhost:3000  # Linux
```

### Option B: Infrastructure via Docker + Local .NET/Node

### 1. Start Services
```bash
docker compose -f docker-compose.dev.yml up -d
```

### 2. Start Backend (database schema auto-created on startup)
```bash
cd backend/src/AgenticShopper.Coordinator
dotnet run
```

Backend will run on: `http://localhost:5050`

### 3. Start Frontend
```bash
cd frontend
npm install
npm run dev
```

Frontend will run on: `http://localhost:5173`

### 4. Access Services
- **Backend API / Swagger**: http://localhost:5050
- **Frontend**: http://localhost:5173
- **RabbitMQ UI**: http://localhost:15672 (admin/admin)
- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`
- **Azurite Blob**: `localhost:10000`

---

## 💡 Tips & Best Practices

1. **Use Docker Desktop GUI** to easily start/stop/view logs
2. **Keep containers running** - they're lightweight and startup time is saved
3. **Use Azure Storage Explorer** for visual blob management
4. **Set up database seeding** for consistent test data
5. **Use PaddleOCR for development** - it's free and works offline
6. **Monitor resource usage** - stop services you're not actively using
7. **Backup volumes** before running `down -v`

---

## 🔄 Migration Path to Azure

When ready to deploy to Azure:

1. **Update `appsettings.Production.json`** with Azure connection strings
2. **Deploy infrastructure** using Bicep/Terraform
3. **Run migrations** on Azure PostgreSQL
4. **Upload existing blobs** from Azurite to Azure Storage
5. **Configure Azure Service Bus** queues/topics
6. **Enable Azure Document Intelligence** for production OCR

The application code works with both local and Azure services without changes! 🎉

---

## 📚 Additional Resources

- [Azurite Documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [Apache Kafka Quickstart](https://kafka.apache.org/quickstart)
- [PaddleOCR Documentation](https://github.com/PaddlePaddle/PaddleOCR)
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)
