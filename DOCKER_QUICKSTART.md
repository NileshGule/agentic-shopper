# Docker Quick Start Guide

This guide will help you build and run the Agentic Shopper project locally using Docker containers.

## Prerequisites

1. **Docker Desktop** installed and running
   - Download: https://www.docker.com/products/docker-desktop
   - Minimum: Docker 20.10+, Docker Compose 2.0+

2. **.NET 10 SDK** (for running migrations locally)
   - Download: https://dotnet.microsoft.com/download/dotnet/10.0
   - Or skip migrations and use the pre-seeded database

3. **Minimum System Requirements**
   - RAM: 8GB (16GB recommended)
   - Disk: 10GB free space
   - CPU: 4 cores recommended

## Quick Start (Automated)

### Option 1: Full Stack with One Command

```bash
# Make scripts executable (first time only)
chmod +x build-and-run-docker.sh stop-docker.sh

# Build and run everything
./build-and-run-docker.sh
```

This script will:
1. ✅ Check Docker is running
2. ✅ Start infrastructure (PostgreSQL, Redis, Azurite)
3. ✅ Run database migrations
4. ✅ Build Docker images (Coordinator + Frontend)
5. ✅ Start application services
6. ✅ Display access URLs

**Access the application:**
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Option 2: Step-by-Step Manual Setup

#### Step 1: Start Infrastructure Services

```bash
# Start PostgreSQL, Redis, Azurite, RabbitMQ, Kafka
docker-compose -f docker-compose.dev.yml up -d

# Check status
docker-compose -f docker-compose.dev.yml ps
```

#### Step 2: Run Database Migrations

```bash
cd backend/src/AgenticShopper.Coordinator

# Apply migrations
dotnet ef database update --project ../AgenticShopper.Data \
  --connection "Host=localhost;Database=agentic_shopper;Username=postgres;Password=postgres;Port=5432"

cd ../../..
```

#### Step 3: Build Docker Images

```bash
# Build Coordinator (backend API)
docker build \
  -f backend/deployment/docker/Dockerfile.coordinator \
  -t agentic-shopper-coordinator:latest \
  ./backend

# Build Frontend (React app)
docker build \
  -f frontend/Dockerfile \
  -t agentic-shopper-frontend:latest \
  ./frontend
```

#### Step 4: Create Environment Configuration

Create a `.env` file in the project root:

```bash
# PostgreSQL Configuration
POSTGRES_DB=agentic_shopper
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# Redis Configuration
REDIS_PASSWORD=
REDIS_PORT=6379

# Azure Storage (using Azurite emulator)
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;
AZURE_STORAGE_CONTAINER=receipts

# Azure Document Intelligence (Optional - leave empty for mock OCR)
AZURE_DOC_INTELLIGENCE_ENDPOINT=
AZURE_DOC_INTELLIGENCE_KEY=

# LLM Configuration
LLM_PROVIDER=FoundryLocal
LLM_ENDPOINT=http://localhost:8080
LLM_MODEL=phi-3-mini

# JWT Configuration
JWT_SECRET_KEY=YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!
JWT_ISSUER=AgenticShopper
JWT_AUDIENCE=AgenticShopperUsers

# Application URLs
FRONTEND_URL=http://localhost:3000
API_URL=http://localhost:5000
COORDINATOR_PORT=5000
FRONTEND_PORT=3000

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

#### Step 5: Start Application Services

```bash
cd backend/deployment/docker
docker-compose up -d
cd ../../..
```

## Verify Installation

### Check Running Containers

```bash
docker ps --filter "name=agentic-shopper"
```

Expected output:
```
CONTAINER ID   IMAGE                              STATUS         PORTS
abc123...      agentic-shopper-frontend:latest    Up 2 minutes   0.0.0.0:3000->80/tcp
def456...      agentic-shopper-coordinator:latest Up 2 minutes   0.0.0.0:5000->80/tcp
ghi789...      postgres:15-alpine                 Up 3 minutes   0.0.0.0:5432->5432/tcp
jkl012...      redis:7-alpine                     Up 3 minutes   0.0.0.0:6379->6379/tcp
```

### Test Services

```bash
# Test PostgreSQL
docker exec agentic-shopper-postgres pg_isready -U postgres

# Test Redis
docker exec agentic-shopper-redis redis-cli PING

# Test API
curl http://localhost:5000/health

# Test Frontend
curl http://localhost:3000
```

## Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | - |
| **API** | http://localhost:5000 | JWT token required |
| **Swagger UI** | http://localhost:5000/swagger | - |
| **PostgreSQL** | localhost:5432 | user: `postgres`, password: `postgres`, db: `agentic_shopper` |
| **Redis** | localhost:6379 | No password |
| **Azurite Blob** | http://localhost:10000 | Account: `devstoreaccount1` |
| **RabbitMQ UI** | http://localhost:15672 | user: `admin`, password: `admin` |

## Useful Commands

### View Logs

```bash
# All services
docker-compose -f backend/deployment/docker/docker-compose.yml logs -f

# Specific service
docker logs -f agentic-shopper-coordinator
docker logs -f agentic-shopper-frontend

# Infrastructure services
docker-compose -f docker-compose.dev.yml logs -f
```

### Stop Services

```bash
# Automated
./stop-docker.sh

# Manual
docker-compose -f backend/deployment/docker/docker-compose.yml down
docker-compose -f docker-compose.dev.yml down
```

### Restart Services

```bash
# Restart all
docker-compose -f backend/deployment/docker/docker-compose.yml restart

# Restart specific service
docker restart agentic-shopper-coordinator
```

### Clean Up Everything

```bash
# Stop and remove containers
docker-compose -f backend/deployment/docker/docker-compose.yml down
docker-compose -f docker-compose.dev.yml down

# Remove volumes (deletes all data!)
docker-compose -f backend/deployment/docker/docker-compose.yml down -v
docker-compose -f docker-compose.dev.yml down -v

# Remove images
docker rmi agentic-shopper-coordinator:latest
docker rmi agentic-shopper-frontend:latest
```

## Troubleshooting

### Issue: Port Already in Use

```bash
# Check what's using the port
lsof -i :5000  # or :3000, :5432, etc.

# Kill the process
kill -9 <PID>

# Or change ports in .env file
```

### Issue: Database Migration Fails

```bash
# Drop and recreate database
docker exec -it agentic-shopper-postgres psql -U postgres -c "DROP DATABASE IF EXISTS agentic_shopper;"
docker exec -it agentic-shopper-postgres psql -U postgres -c "CREATE DATABASE agentic_shopper;"

# Rerun migrations
cd backend/src/AgenticShopper.Coordinator
dotnet ef database update --project ../AgenticShopper.Data \
  --connection "Host=localhost;Database=agentic_shopper;Username=postgres;Password=postgres"
```

### Issue: Container Won't Start

```bash
# Check logs
docker logs agentic-shopper-coordinator

# Check if container exists
docker ps -a | grep agentic-shopper

# Remove and recreate
docker rm -f agentic-shopper-coordinator
docker-compose -f backend/deployment/docker/docker-compose.yml up -d coordinator
```

### Issue: Frontend Can't Connect to API

1. Check CORS settings in `backend/src/AgenticShopper.Coordinator/Program.cs`
2. Verify `FRONTEND_URL` in `.env` matches your frontend URL
3. Check browser console for CORS errors
4. Ensure API is running: `curl http://localhost:5000/health`

### Issue: Out of Disk Space

```bash
# Clean up Docker
docker system prune -a --volumes

# This removes:
# - All stopped containers
# - All networks not used by containers
# - All images without at least one container
# - All build cache
# - All volumes not used by containers
```

## Development Workflow

### Making Code Changes

**Backend Changes:**
1. Make changes to C# code
2. Rebuild image: `docker build -f backend/deployment/docker/Dockerfile.coordinator -t agentic-shopper-coordinator:latest ./backend`
3. Restart container: `docker restart agentic-shopper-coordinator`

**Frontend Changes:**
1. Make changes to React code
2. Rebuild image: `docker build -f frontend/Dockerfile -t agentic-shopper-frontend:latest ./frontend`
3. Restart container: `docker restart agentic-shopper-frontend`

### Hot Reload (Alternative)

For faster development, run services locally instead of in containers:

```bash
# Start infrastructure only
docker-compose -f docker-compose.dev.yml up -d

# Run backend locally
cd backend/src/AgenticShopper.Coordinator
dotnet run

# Run frontend locally (in another terminal)
cd frontend
npm install
npm start
```

## Next Steps

1. **Create a test user**: Use the `/api/auth/login` endpoint
2. **Upload a receipt**: Navigate to http://localhost:3000 and upload a test receipt
3. **Explore the API**: Visit http://localhost:5000/swagger

## Performance Tips

- **Allocate more memory to Docker**: Docker Desktop Settings → Resources → Memory (8GB recommended)
- **Enable BuildKit**: `export DOCKER_BUILDKIT=1` for faster builds
- **Use .dockerignore**: Already configured to exclude node_modules, bin, obj

## Production Deployment

For production deployment, see:
- [Kubernetes Deployment](backend/deployment/kubernetes/README.md)
- [Azure Container Apps](backend/deployment/azure/README.md)

## Support

If you encounter issues:
1. Check logs: `docker logs agentic-shopper-coordinator`
2. Review [DEVELOPMENT.md](DEVELOPMENT.md) for detailed setup
3. Check [README.md](README.md) for architecture overview
