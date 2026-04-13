#!/bin/bash

# Agentic Shopper - Stop Docker Services Script

echo "🛑 Stopping Agentic Shopper Docker Services..."
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Stop application services
echo "Stopping application services..."
cd backend/deployment/docker
docker-compose down
cd ../../..

# Stop infrastructure services
echo "Stopping infrastructure services..."
docker-compose -f docker-compose.dev.yml down

echo ""
echo -e "${GREEN}✅ All services stopped${NC}"
echo ""
echo -e "${YELLOW}📝 Note:${NC} Data volumes are preserved. To remove volumes and data:"
echo "  docker-compose -f docker-compose.dev.yml down -v"
echo "  docker-compose -f backend/deployment/docker/docker-compose.yml down -v"
echo ""
