# Azure Container Apps Deployment Guide

This directory contains Bicep templates for deploying the Agentic Shopper application to Azure Container Apps.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Azure Container Apps Environment        │
│                                                          │
│  ┌──────────────┐         ┌──────────────────────────┐ │
│  │   Frontend   │         │      Coordinator         │ │
│  │  (React App) │◄────────│   (.NET Container App)   │ │
│  │   2-5 reps   │         │      3-10 replicas       │ │
│  └──────┬───────┘         └──────┬───────────────────┘ │
│         │                         │                     │
└─────────┼─────────────────────────┼─────────────────────┘
          │                         │
          └────────┬────────────────┘
                   │
         ┌─────────▼──────────┐
         │  External Services  │
         ├────────────────────┤
         │ PostgreSQL Flexible│
         │ Redis Cache        │
         │ Blob Storage       │
         │ App Insights       │
         │ Container Registry │
         └────────────────────┘
```

## Resources Created

| Resource | Purpose | SKU |
|----------|---------|-----|
| **Container Apps Environment** | Hosting platform | Consumption |
| **Coordinator Container App** | Backend API + Agents | 1 CPU, 2Gi RAM (per replica) |
| **Frontend Container App** | React UI | 0.5 CPU, 1Gi RAM (per replica) |
| **PostgreSQL Flexible Server** | Database | Standard_D2s_v3, 32GB storage |
| **Azure Cache for Redis** | Caching | Standard C1 (1GB) |
| **Storage Account** | Blob storage for receipts | Standard_ZRS (prod), Standard_LRS (dev) |
| **Container Registry** | Docker images | Standard |
| **Log Analytics** | Logging and monitoring | PerGB2018 |
| **Application Insights** | Telemetry and APM | Standard |

## Prerequisites

1. **Azure CLI** installed and authenticated
2. **Azure subscription** with appropriate permissions
3. **Resource group** created
4. **Azure Key Vault** (for production secrets)
5. **Docker images** pushed to GitHub Container Registry

## Quick Start

### 1. Create Resource Group

```bash
az group create \
  --name agentic-shopper-rg \
  --location australiaeast
```

### 2. Validate Bicep Template

```bash
az deployment group validate \
  --resource-group agentic-shopper-rg \
  --template-file main.bicep \
  --parameters @parameters.dev.json
```

### 3. Deploy to Development

```bash
# Update parameters.dev.json with your secrets first!

az deployment group create \
  --resource-group agentic-shopper-rg \
  --template-file main.bicep \
  --parameters @parameters.dev.json \
  --name agentic-shopper-dev-$(date +%Y%m%d-%H%M%S)
```

### 4. Deploy to Production

```bash
# For production, use Azure Key Vault for secrets
# See parameters.json for KeyVault reference format

az deployment group create \
  --resource-group agentic-shopper-rg \
  --template-file main.bicep \
  --parameters @parameters.json \
  --name agentic-shopper-prod-$(date +%Y%m%d-%H%M%S)
```

## Deployment Parameters

### Required Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `appName` | Application name prefix | `agentic-shopper` |
| `environment` | Environment (dev/staging/prod) | `prod` |
| `location` | Azure region | `australiaeast` |
| `postgresAdminUsername` | PostgreSQL admin user | `pgadmin` |
| `postgresAdminPassword` | PostgreSQL admin password | `<secure-password>` |
| `jwtSecretKey` | JWT signing key (256-bit) | `<base64-encoded-key>` |
| `llmEndpoint` | Azure OpenAI endpoint | `https://your-foundry.openai.azure.com/` |
| `llmApiKey` | Azure OpenAI API key | `<api-key>` |
| `documentIntelligenceEndpoint` | Document Intelligence endpoint | `https://your-region.api.cognitive.microsoft.com/` |
| `documentIntelligenceApiKey` | Document Intelligence API key | `<api-key>` |

### Optional Parameters

| Parameter | Description | Default | Production |
|-----------|-------------|---------|-----------|
| `imageTag` | Container image tag | `latest` | `v1.0.0` |
| `coordinatorMinReplicas` | Min coordinator replicas | `3` | `3` |
| `coordinatorMaxReplicas` | Max coordinator replicas | `10` | `10` |
| `enableApplicationInsights` | Enable App Insights | `true` | `true` |
| `enableZoneRedundancy` | Enable zone redundancy | `true` | `true` |

## Generating Secrets

### JWT Secret Key

```bash
# Generate 256-bit key
JWT_SECRET=$(openssl rand -base64 32)
echo "JwtSettings__SecretKey: $JWT_SECRET"
```

### PostgreSQL Password

```bash
# Generate secure password
POSTGRES_PASSWORD=$(openssl rand -base64 24)
echo "PostgreSQL Password: $POSTGRES_PASSWORD"
```

## Using Azure Key Vault (Production)

### 1. Create Key Vault

```bash
az keyvault create \
  --name agentic-shopper-kv-$RANDOM \
  --resource-group agentic-shopper-rg \
  --location australiaeast \
  --enable-rbac-authorization false
```

### 2. Store Secrets

```bash
# PostgreSQL password
az keyvault secret set \
  --vault-name agentic-shopper-kv-12345 \
  --name postgres-admin-password \
  --value "your-secure-password"

# JWT secret
az keyvault secret set \
  --vault-name agentic-shopper-kv-12345 \
  --name jwt-secret-key \
  --value "$(openssl rand -base64 32)"

# LLM API key
az keyvault secret set \
  --vault-name agentic-shopper-kv-12345 \
  --name llm-api-key \
  --value "your-llm-api-key"

# Document Intelligence key
az keyvault secret set \
  --vault-name agentic-shopper-kv-12345 \
  --name document-intelligence-api-key \
  --value "your-doc-intel-key"
```

### 3. Update parameters.json

Replace placeholder values with Key Vault references:

```json
{
  "postgresAdminPassword": {
    "reference": {
      "keyVault": {
        "id": "/subscriptions/{sub-id}/resourceGroups/agentic-shopper-rg/providers/Microsoft.KeyVault/vaults/agentic-shopper-kv-12345"
      },
      "secretName": "postgres-admin-password"
    }
  }
}
```

## Post-Deployment

### 1. Get Application URLs

```bash
# Get output values
az deployment group show \
  --resource-group agentic-shopper-rg \
  --name agentic-shopper-prod-<timestamp> \
  --query properties.outputs

# Or query directly
COORDINATOR_URL=$(az containerapp show \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

FRONTEND_URL=$(az containerapp show \
  --name agentic-shopper-prod-frontend \
  --resource-group agentic-shopper-rg \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "Coordinator: https://$COORDINATOR_URL"
echo "Frontend: https://$FRONTEND_URL"
```

### 2. Configure Custom Domain (Optional)

```bash
# Add custom domain to coordinator
az containerapp hostname add \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --hostname api.yourdomain.com

# Add custom domain to frontend
az containerapp hostname add \
  --name agentic-shopper-prod-frontend \
  --resource-group agentic-shopper-rg \
  --hostname www.yourdomain.com
```

### 3. Run Database Migrations

```bash
# Get PostgreSQL connection details
POSTGRES_HOST=$(az deployment group show \
  --resource-group agentic-shopper-rg \
  --name agentic-shopper-prod-<timestamp> \
  --query properties.outputs.postgresServerFqdn.value \
  --output tsv)

# Connect and run migrations
psql "postgresql://pgadmin:<password>@$POSTGRES_HOST:5432/agentic_shopper?sslmode=require"

# Or use EF Core migrations from local machine
dotnet ef database update --connection "Host=$POSTGRES_HOST;Database=agentic_shopper;Username=pgadmin;Password=<password>;SSL Mode=Require"
```

## Monitoring

### View Application Logs

```bash
# Coordinator logs
az containerapp logs show \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --follow

# Frontend logs
az containerapp logs show \
  --name agentic-shopper-prod-frontend \
  --resource-group agentic-shopper-rg \
  --follow
```

### Application Insights

```bash
# Get Application Insights key
APPINSIGHTS_KEY=$(az deployment group show \
  --resource-group agentic-shopper-rg \
  --name agentic-shopper-prod-<timestamp> \
  --query properties.outputs.applicationInsightsKey.value \
  --output tsv)

# Open in Azure Portal
echo "https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AzureMonitoringBrowseBlade/overview"
```

### Metrics

```bash
# View replica count
az containerapp replica list \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg

# View revision details
az containerapp revision list \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --output table
```

## Scaling

### Manual Scaling

```bash
# Update min/max replicas
az containerapp update \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --min-replicas 5 \
  --max-replicas 20
```

### View Scaling Rules

```bash
az containerapp show \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --query properties.template.scale
```

## Updating Application

### Deploy New Image Version

```bash
# Update coordinator image
az containerapp update \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --image ghcr.io/nileshgule/agentic-shopper-coordinator:v1.1.0

# Update frontend image
az containerapp update \
  --name agentic-shopper-prod-frontend \
  --resource-group agentic-shopper-rg \
  --image ghcr.io/nileshgule/agentic-shopper-frontend:v1.1.0
```

### Rollback Revision

```bash
# List revisions
az containerapp revision list \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --output table

# Activate previous revision
az containerapp revision activate \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --revision <revision-name>
```

## Cost Optimization

### Development Environment

- Use `environment: dev` parameter
- Disable zone redundancy (`enableZoneRedundancy: false`)
- Disable Application Insights (`enableApplicationInsights: false`)
- Use single replicas (`minReplicas: 1, maxReplicas: 2`)
- Use Standard_LRS for storage
- Reduce PostgreSQL backup retention to 7 days

Estimated cost: ~$150-200/month

### Production Environment

- Use `environment: prod` parameter
- Enable zone redundancy (`enableZoneRedundancy: true`)
- Enable Application Insights (`enableApplicationInsights: true`)
- Use multiple replicas (`minReplicas: 3, maxReplicas: 10`)
- Use Standard_ZRS for storage
- Enable PostgreSQL high availability
- Set backup retention to 35 days

Estimated cost: ~$600-800/month (excluding LLM API costs)

## Cleanup

```bash
# Delete resource group and all resources
az group delete \
  --name agentic-shopper-rg \
  --yes --no-wait
```

## Troubleshooting

### Container App Not Starting

```bash
# Check replica status
az containerapp replica list \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg

# View detailed logs
az containerapp logs show \
  --name agentic-shopper-prod-coordinator \
  --resource-group agentic-shopper-rg \
  --tail 100
```

### Database Connection Issues

```bash
# Test PostgreSQL connectivity
az postgres flexible-server connect \
  --name agentic-shopper-prod-postgres-<suffix> \
  --resource-group agentic-shopper-rg \
  --admin-user pgadmin \
  --admin-password <password> \
  --database-name agentic_shopper
```

### Redis Connection Issues

```bash
# Get Redis keys
az redis list-keys \
  --name agentic-shopper-prod-redis-<suffix> \
  --resource-group agentic-shopper-rg

# Test Redis connectivity
redis-cli -h agentic-shopper-prod-redis-<suffix>.redis.cache.windows.net \
  -p 6380 -a <primary-key> --tls ping
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Deploy to Azure Container Apps
  uses: azure/container-apps-deploy-action@v1
  with:
    resourceGroup: agentic-shopper-rg
    containerAppName: agentic-shopper-prod-coordinator
    imageToDeploy: ghcr.io/nileshgule/agentic-shopper-coordinator:${{ github.sha }}
```

## Security Best Practices

1. **Use Azure Key Vault** for all secrets in production
2. **Enable managed identities** for Container Apps
3. **Restrict network access** using VNet integration
4. **Enable TLS/SSL** for all external endpoints
5. **Use Azure Front Door** for additional security and CDN
6. **Enable Azure DDoS Protection** for production
7. **Implement rate limiting** in Container App ingress
8. **Regular security updates** for container images
9. **Enable diagnostic logging** to Log Analytics
10. **Use Azure Policy** for compliance enforcement

## Support

For issues or questions:
- GitHub Issues: https://github.com/NileshGule/agentic-shopper/issues
- Documentation: /specs/001-shopping-analyzer/
