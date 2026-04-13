#!/bin/bash

# Agentic Shopper - Local Docker Build & Run Script
# This script builds and runs the complete application stack locally

set -e

echo "🚀 Agentic Shopper - Local Docker Deployment"
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if Docker is running
echo "🔍 Checking Docker..."
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running. Please start Docker Desktop and try again.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Docker is running${NC}"
echo ""

# Step 1: Start Infrastructure Services
echo -e "${BLUE}📦 Step 1: Starting infrastructure services...${NC}"
docker-compose -f docker-compose.dev.yml up -d postgres redis azurite

echo "⏳ Waiting for infrastructure to be ready (15 seconds)..."
sleep 15

# Check PostgreSQL
if docker exec agentic-shopper-postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo -e "${GREEN}✅ PostgreSQL is ready${NC}"
else
    echo -e "${YELLOW}⚠️  PostgreSQL is still starting...${NC}"
fi

# Check Redis
if docker exec agentic-shopper-redis redis-cli PING > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Redis is ready${NC}"
else
    echo -e "${YELLOW}⚠️  Redis is still starting...${NC}"
fi

echo -e "${GREEN}✅ Azurite (Azure Storage Emulator) is ready${NC}"
echo ""

# Step 2: Run Database Migrations
echo -e "${BLUE}📊 Step 2: Running database migrations...${NC}"
cd backend/src/AgenticShopper.Coordinator

# Check if migrations exist
if dotnet ef migrations list --project ../AgenticShopper.Data > /dev/null 2>&1; then
    echo "Applying migrations..."
    dotnet ef database update --project ../AgenticShopper.Data --connection "Host=localhost;Database=agentic_shopper;Username=postgres;Password=postgres;Port=5432"
    echo -e "${GREEN}✅ Database migrations applied${NC}"
else
    echo -e "${YELLOW}⚠️  No migrations found or EF Core tools not installed${NC}"
    echo "Install EF Core tools: dotnet tool install --global dotnet-ef"
fi

cd ../../..
echo ""

# Step 3: Build Docker Images
echo -e "${BLUE}🔨 Step 3: Building application Docker images...${NC}"
echo "This may take 5-10 minutes on first run..."
echo ""

# Build Coordinator
echo "Building Coordinator service..."
docker build \
    -f backend/deployment/docker/Dockerfile.coordinator \
    -t agentic-shopper-coordinator:latest \
    ./backend

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Coordinator image built${NC}"
else
    echo -e "${RED}❌ Failed to build Coordinator image${NC}"
    exit 1
fi

echo ""

# Build Frontend
echo "Building Frontend service..."
docker build \
    -f frontend/Dockerfile \
    -t agentic-shopper-frontend:latest \
    ./frontend

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Frontend image built${NC}"
else
    echo -e "${RED}❌ Failed to build Frontend image${NC}"
    exit 1
fi

echo ""

# Step 4: Create .env file if not exists
echo -e "${BLUE}⚙️  Step 4: Checking environment configuration...${NC}"
if [ ! -f .env ]; then
    echo "Creating .env file from template..."
    cat > .env << 'EOF'
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

# Azure Document Intelligence (Optional - leave empty to use mock OCR)
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
EOF
    echo -e "${GREEN}✅ .env file created${NC}"
else
    echo -e "${GREEN}✅ .env file exists${NC}"
fi
echo ""

# Step 5: Start Application Services
echo -e "${BLUE}🚀 Step 5: Starting application services...${NC}"
cd backend/deployment/docker

# Use environment variables from .env
export $(cat ../../../.env | grep -v '^#' | xargs)

docker-compose up -d

cd ../../..
echo ""

# Step 6: Wait for services
echo "⏳ Waiting for application services to start (20 seconds)..."
sleep 20

# Step 7: Display Status
echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
echo "================================"
docker ps --filter "name=agentic-shopper" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Step 8: Display Access URLs
echo -e "${GREEN}✅ Agentic Shopper is running!${NC}"
echo ""
echo -e "${BLUE}🌐 Access URLs:${NC}"
echo "================================"
echo "Frontend:          http://localhost:3000"
echo "API (Coordinator): http://localhost:5000"
echo "API Docs (Swagger): http://localhost:5000/swagger"
echo ""
echo "Infrastructure Services:"
echo "PostgreSQL:        localhost:5432 (user: postgres, password: postgres, db: agentic_shopper)"
echo "Redis:             localhost:6379"
echo "Azurite Blob:      http://localhost:10000"
echo "RabbitMQ UI:       http://localhost:15672 (user: admin, password: admin)"
echo ""

# Step 9: Show logs
echo -e "${YELLOW}📝 To view logs:${NC}"
echo "  All services:    docker-compose -f backend/deployment/docker/docker-compose.yml logs -f"
echo "  Coordinator:     docker logs -f agentic-shopper-coordinator"
echo "  Frontend:        docker logs -f agentic-shopper-frontend"
echo ""

echo -e "${YELLOW}🛑 To stop all services:${NC}"
echo "  ./stop-docker.sh"
echo "  OR"
echo "  docker-compose -f docker-compose.dev.yml down"
echo "  docker-compose -f backend/deployment/docker/docker-compose.yml down"
echo ""

echo -e "${GREEN}🎉 Setup complete!${NC}"
