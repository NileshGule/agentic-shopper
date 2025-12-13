#!/bin/bash

# Agentic Shopper Local Development Startup Script
# This script starts all local development services using Docker

set -e

echo "🚀 Starting Agentic Shopper Local Development Environment..."
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

echo "✅ Docker is running"
echo ""

# Start services
echo "📦 Starting local services with Docker Compose..."
docker-compose -f docker-compose.dev.yml up -d

echo ""
echo "⏳ Waiting for services to be healthy..."
sleep 5

# Check service health
echo ""
echo "🔍 Checking service status..."
echo ""

# PostgreSQL
if docker exec agentic-shopper-postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo -e "${GREEN}✅ PostgreSQL${NC} - Ready on port 5432"
else
    echo -e "${YELLOW}⚠️  PostgreSQL${NC} - Not ready yet (starting...)"
fi

# Redis
if docker exec agentic-shopper-redis redis-cli PING > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Redis${NC} - Ready on port 6379"
else
    echo -e "${YELLOW}⚠️  Redis${NC} - Not ready yet (starting...)"
fi

# RabbitMQ
if docker exec agentic-shopper-rabbitmq rabbitmq-diagnostics ping > /dev/null 2>&1; then
    echo -e "${GREEN}✅ RabbitMQ${NC} - Ready on port 5672 (Management UI: http://localhost:15672)"
else
    echo -e "${YELLOW}⚠️  RabbitMQ${NC} - Not ready yet (starting...)"
fi

# Azurite
if nc -z localhost 10000 2>/dev/null; then
    echo -e "${GREEN}✅ Azurite${NC} - Ready on ports 10000-10002"
else
    echo -e "${YELLOW}⚠️  Azurite${NC} - Not ready yet (starting...)"
fi

# Kafka
if nc -z localhost 9092 2>/dev/null; then
    echo -e "${GREEN}✅ Kafka${NC} - Ready on port 9092"
else
    echo -e "${YELLOW}⚠️  Kafka${NC} - Not ready yet (starting...)"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Local development environment is starting!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📊 Service URLs:"
echo "   • RabbitMQ Management: http://localhost:15672 (admin/admin)"
echo "   • PostgreSQL: localhost:5432 (postgres/postgres)"
echo "   • Redis: localhost:6379"
echo "   • Azurite Blob: http://localhost:10000"
echo "   • Kafka: localhost:9092"
echo ""
echo "📝 Next Steps:"
echo "   1. Run database migrations:"
echo "      cd backend/src/AgenticShopper.Coordinator"
echo "      dotnet ef database update"
echo ""
echo "   2. Start the backend:"
echo "      dotnet run"
echo ""
echo "   3. Start the frontend:"
echo "      cd frontend"
echo "      npm run dev"
echo ""
echo "📖 For more details, see DEVELOPMENT.md"
echo ""
echo "🛑 To stop all services: docker-compose -f docker-compose.dev.yml down"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
