# Quick Docker Setup - Step by Step Commands

## Prerequisites Check
```bash
# 1. Check Docker is running
docker --version
docker-compose --version
```

## Step-by-Step Execution

### Step 1: Start Infrastructure Services (PostgreSQL, Redis, Azurite)
```bash
cd /Users/nilesh/projects/agentic-shopper

# Start infrastructure
docker-compose -f docker-compose.dev.yml up -d postgres redis azurite

# Wait 15 seconds for services to start
sleep 15

# Check status
docker ps --filter "name=agentic-shopper"
```

### Step 2: Verify Infrastructure
```bash
# Test PostgreSQL
docker exec agentic-shopper-postgres pg_isready -U postgres

# Test Redis
docker exec agentic-shopper-redis redis-cli PING

# Should see "PONG"
```

### Step 3: Run Database Migrations
```bash
cd backend/src/AgenticShopper.Coordinator

# Apply migrations (creates tables and seeds data)
dotnet ef database update --project ../AgenticShopper.Data \
  --connection "Host=localhost;Database=agentic_shopper;Username=postgres;Password=postgres;Port=5432"

cd ../../..
```

### Step 4: Build Backend Docker Image
```bash
# This takes 5-10 minutes first time
docker build \
  -f backend/deployment/docker/Dockerfile.coordinator \
  -t agentic-shopper-coordinator:latest \
  ./backend

# Verify image was created
docker images | grep agentic-shopper-coordinator
```

### Step 5: Build Frontend Docker Image
```bash
# This takes 3-5 minutes first time
docker build \
  -f frontend/Dockerfile \
  -t agentic-shopper-frontend:latest \
  ./frontend

# Verify image was created
docker images | grep agentic-shopper-frontend
```

### Step 6: Create Environment File
```bash
# Create .env file in project root
cat > .env << 'EOF'
POSTGRES_DB=agentic_shopper
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

REDIS_PASSWORD=
REDIS_PORT=6379

AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;
AZURE_STORAGE_CONTAINER=receipts

AZURE_DOC_INTELLIGENCE_ENDPOINT=
AZURE_DOC_INTELLIGENCE_KEY=

LLM_PROVIDER=FoundryLocal
LLM_ENDPOINT=http://localhost:8080
LLM_MODEL=phi-3-mini

JWT_SECRET_KEY=YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!
JWT_ISSUER=AgenticShopper
JWT_AUDIENCE=AgenticShopperUsers

FRONTEND_URL=http://localhost:3000
API_URL=http://localhost:5000
COORDINATOR_PORT=5000
FRONTEND_PORT=3000

ASPNETCORE_ENVIRONMENT=Development
EOF

echo "✅ .env file created"
```

### Step 7: Start Application Services
```bash
cd backend/deployment/docker

# Load environment variables and start services
docker-compose up -d

cd ../../..

# Wait for services to start
sleep 20
```

### Step 8: Verify Everything is Running
```bash
# Check all containers
docker ps --filter "name=agentic-shopper" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Check coordinator logs
docker logs agentic-shopper-coordinator --tail 50

# Check frontend logs
docker logs agentic-shopper-frontend --tail 50
```

### Step 9: Test Access
```bash
# Test API health endpoint
curl http://localhost:5000/health

# Test frontend (should return HTML)
curl http://localhost:3000

# Open in browser
open http://localhost:3000  # macOS
# or visit http://localhost:3000 in your browser
```

## Access URLs

✅ **Frontend**: http://localhost:3000
✅ **API**: http://localhost:5000
✅ **Swagger UI**: http://localhost:5000/swagger
✅ **PostgreSQL**: localhost:5432 (user: postgres, password: postgres)
✅ **Redis**: localhost:6379

## View Logs

```bash
# Follow all logs
docker-compose -f backend/deployment/docker/docker-compose.yml logs -f

# Follow specific service
docker logs -f agentic-shopper-coordinator
docker logs -f agentic-shopper-frontend
```

## Stop Everything

```bash
# Stop application services
cd backend/deployment/docker
docker-compose down
cd ../../..

# Stop infrastructure services
docker-compose -f docker-compose.dev.yml down
```

## Troubleshooting

### If build fails
```bash
# Check Docker has enough memory (8GB recommended)
# Docker Desktop → Settings → Resources → Memory

# Clean build cache and retry
docker builder prune -a
```

### If services won't start
```bash
# Check port availability
lsof -i :5000  # API port
lsof -i :3000  # Frontend port
lsof -i :5432  # PostgreSQL port

# Stop any conflicting services
```

### If database migration fails
```bash
# Ensure PostgreSQL is running
docker ps | grep postgres

# Try running migration with verbose logging
cd backend/src/AgenticShopper.Coordinator
dotnet ef database update --project ../AgenticShopper.Data --verbose
```

### Reset Everything
```bash
# Stop and remove everything including data
docker-compose -f backend/deployment/docker/docker-compose.yml down -v
docker-compose -f docker-compose.dev.yml down -v

# Remove images
docker rmi agentic-shopper-coordinator:latest
docker rmi agentic-shopper-frontend:latest

# Start fresh from Step 1
```
