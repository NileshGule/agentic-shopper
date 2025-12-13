# Agentic Shopper Local Development Startup Script (PowerShell)
# This script starts all local development services using Docker

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Agentic Shopper Local Development Environment..." -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
}
catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

Write-Host ""

# Start services
Write-Host "📦 Starting local services with Docker Compose..." -ForegroundColor Cyan
docker-compose -f docker-compose.dev.yml up -d

Write-Host ""
Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check service health
Write-Host ""
Write-Host "🔍 Checking service status..." -ForegroundColor Cyan
Write-Host ""

# PostgreSQL
try {
    docker exec agentic-shopper-postgres pg_isready -U postgres | Out-Null
    Write-Host "✅ PostgreSQL - Ready on port 5432" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  PostgreSQL - Not ready yet (starting...)" -ForegroundColor Yellow
}

# Redis
try {
    docker exec agentic-shopper-redis redis-cli PING | Out-Null
    Write-Host "✅ Redis - Ready on port 6379" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  Redis - Not ready yet (starting...)" -ForegroundColor Yellow
}

# RabbitMQ
try {
    docker exec agentic-shopper-rabbitmq rabbitmq-diagnostics ping | Out-Null
    Write-Host "✅ RabbitMQ - Ready on port 5672 (Management UI: http://localhost:15672)" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  RabbitMQ - Not ready yet (starting...)" -ForegroundColor Yellow
}

# Azurite
try {
    $connection = Test-NetConnection -ComputerName localhost -Port 10000 -InformationLevel Quiet
    if ($connection) {
        Write-Host "✅ Azurite - Ready on ports 10000-10002" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Azurite - Not ready yet (starting...)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "⚠️  Azurite - Not ready yet (starting...)" -ForegroundColor Yellow
}

# Kafka
try {
    $connection = Test-NetConnection -ComputerName localhost -Port 9092 -InformationLevel Quiet
    if ($connection) {
        Write-Host "✅ Kafka - Ready on port 9092" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Kafka - Not ready yet (starting...)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "⚠️  Kafka - Not ready yet (starting...)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🎉 Local development environment is starting!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Service URLs:" -ForegroundColor Cyan
Write-Host "   • RabbitMQ Management: http://localhost:15672 (admin/admin)"
Write-Host "   • PostgreSQL: localhost:5432 (postgres/postgres)"
Write-Host "   • Redis: localhost:6379"
Write-Host "   • Azurite Blob: http://localhost:10000"
Write-Host "   • Kafka: localhost:9092"
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Run database migrations:"
Write-Host "      cd backend\src\AgenticShopper.Coordinator"
Write-Host "      dotnet ef database update"
Write-Host ""
Write-Host "   2. Start the backend:"
Write-Host "      dotnet run"
Write-Host ""
Write-Host "   3. Start the frontend:"
Write-Host "      cd frontend"
Write-Host "      npm run dev"
Write-Host ""
Write-Host "📖 For more details, see DEVELOPMENT.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "🛑 To stop all services: docker-compose -f docker-compose.dev.yml down" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
