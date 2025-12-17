// Main Bicep template for deploying Agentic Shopper to Azure Container Apps
// Deploy: az deployment group create --resource-group agentic-shopper-rg --template-file main.bicep --parameters @parameters.json

targetScope = 'resourceGroup'

@description('Application name prefix')
param appName string = 'agentic-shopper'

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'prod'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Container image tag')
param imageTag string = 'latest'

@description('PostgreSQL administrator username')
param postgresAdminUsername string

@secure()
@description('PostgreSQL administrator password')
param postgresAdminPassword string

@secure()
@description('JWT secret key (256-bit base64 encoded)')
param jwtSecretKey string

@description('Azure OpenAI/AI Foundry endpoint')
param llmEndpoint string

@secure()
@description('Azure OpenAI/AI Foundry API key')
param llmApiKey string

@description('Azure Document Intelligence endpoint')
param documentIntelligenceEndpoint string

@secure()
@description('Azure Document Intelligence API key')
param documentIntelligenceApiKey string

@description('Minimum number of replicas for coordinator')
@minValue(1)
@maxValue(30)
param coordinatorMinReplicas int = 3

@description('Maximum number of replicas for coordinator')
@minValue(1)
@maxValue(30)
param coordinatorMaxReplicas int = 10

@description('Enable Application Insights')
param enableApplicationInsights bool = true

@description('Enable zone redundancy')
param enableZoneRedundancy bool = true

// Variables
var uniqueSuffix = substring(uniqueString(resourceGroup().id), 0, 6)
var resourcePrefix = '${appName}-${environment}'

// Tags
var commonTags = {
  Application: appName
  Environment: environment
  ManagedBy: 'Bicep'
  DeploymentTime: utcNow()
}

// ===== Log Analytics Workspace =====
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${resourcePrefix}-logs-${uniqueSuffix}'
  location: location
  tags: commonTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environment == 'prod' ? 90 : 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ===== Application Insights =====
resource appInsights 'Microsoft.Insights/components@2020-02-02' = if (enableApplicationInsights) {
  name: '${resourcePrefix}-insights-${uniqueSuffix}'
  location: location
  tags: commonTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: environment == 'prod' ? 90 : 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ===== Container Registry =====
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: '${replace(resourcePrefix, '-', '')}acr${uniqueSuffix}'
  location: location
  tags: commonTags
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: enableZoneRedundancy ? 'Enabled' : 'Disabled'
  }
}

// ===== Azure Database for PostgreSQL Flexible Server =====
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: '${resourcePrefix}-postgres-${uniqueSuffix}'
  location: location
  tags: commonTags
  sku: {
    name: 'Standard_D2s_v3'
    tier: 'GeneralPurpose'
  }
  properties: {
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    version: '15'
    storage: {
      storageSizeGB: 32
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: environment == 'prod' ? 35 : 7
      geoRedundantBackup: environment == 'prod' ? 'Enabled' : 'Disabled'
    }
    highAvailability: environment == 'prod' ? {
      mode: 'ZoneRedundant'
    } : {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// PostgreSQL Database
resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: 'agentic_shopper'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// PostgreSQL Firewall Rule (allow Azure services)
resource postgresFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ===== Azure Cache for Redis =====
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${resourcePrefix}-redis-${uniqueSuffix}'
  location: location
  tags: commonTags
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
      'maxmemory-delta': '10'
      'maxmemory-reserved': '10'
    }
    redisVersion: '6'
  }
}

// ===== Azure Blob Storage =====
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${replace(resourcePrefix, '-', '')}st${uniqueSuffix}'
  location: location
  tags: commonTags
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Enabled'
  }
}

// Blob Service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 30 : 7
    }
  }
}

// Receipts Container
resource receiptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'receipts'
  properties: {
    publicAccess: 'None'
  }
}

// ===== Container Apps Environment =====
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${resourcePrefix}-env-${uniqueSuffix}'
  location: location
  tags: commonTags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: enableZoneRedundancy
  }
}

// ===== Coordinator Container App =====
resource coordinatorApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${resourcePrefix}-coordinator'
  location: location
  tags: commonTags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        {
          name: 'database-connection-string'
          value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Port=5432;Database=agentic_shopper;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
        }
        {
          name: 'jwt-secret-key'
          value: jwtSecretKey
        }
        {
          name: 'llm-api-key'
          value: llmApiKey
        }
        {
          name: 'document-intelligence-api-key'
          value: documentIntelligenceApiKey
        }
        {
          name: 'azure-storage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'redis-connection-string'
          value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
        {
          name: 'application-insights-key'
          value: enableApplicationInsights ? appInsights.properties.InstrumentationKey : ''
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'coordinator'
          image: 'ghcr.io/nileshgule/agentic-shopper-coordinator:${imageTag}'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Staging'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'database-connection-string'
            }
            {
              name: 'JwtSettings__SecretKey'
              secretRef: 'jwt-secret-key'
            }
            {
              name: 'JwtSettings__Issuer'
              value: 'AgenticShopper'
            }
            {
              name: 'JwtSettings__Audience'
              value: 'AgenticShopperUsers'
            }
            {
              name: 'JwtSettings__ExpiresInMinutes'
              value: '60'
            }
            {
              name: 'LLM__Provider'
              value: 'AzureAIFoundry'
            }
            {
              name: 'LLM__Endpoint'
              value: llmEndpoint
            }
            {
              name: 'LLM__ApiKey'
              secretRef: 'llm-api-key'
            }
            {
              name: 'AzureDocumentIntelligence__Endpoint'
              value: documentIntelligenceEndpoint
            }
            {
              name: 'AzureDocumentIntelligence__ApiKey'
              secretRef: 'document-intelligence-api-key'
            }
            {
              name: 'AzureStorage__ConnectionString'
              secretRef: 'azure-storage-connection-string'
            }
            {
              name: 'AzureStorage__ContainerName'
              value: 'receipts'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'Redis__InstanceName'
              value: 'AgenticShopper:'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: enableApplicationInsights ? appInsights.properties.ConnectionString : ''
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 80
              }
              initialDelaySeconds: 30
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 80
              }
              initialDelaySeconds: 10
              periodSeconds: 5
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: coordinatorMinReplicas
        maxReplicas: coordinatorMaxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
          {
            name: 'cpu-scaling'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          }
        ]
      }
    }
  }
}

// ===== Frontend Container App =====
resource frontendApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${resourcePrefix}-frontend'
  location: location
  tags: commonTags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: 'ghcr.io/nileshgule/agentic-shopper-frontend:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'API_BASE_URL'
              value: 'https://${coordinatorApp.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 2
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '200'
              }
            }
          }
        ]
      }
    }
  }
}

// ===== Outputs =====
@description('Coordinator app URL')
output coordinatorUrl string = 'https://${coordinatorApp.properties.configuration.ingress.fqdn}'

@description('Frontend app URL')
output frontendUrl string = 'https://${frontendApp.properties.configuration.ingress.fqdn}'

@description('PostgreSQL server FQDN')
output postgresServerFqdn string = postgresServer.properties.fullyQualifiedDomainName

@description('Redis hostname')
output redisHostname string = redisCache.properties.hostName

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Container registry login server')
output containerRegistryLoginServer string = containerRegistry.properties.loginServer

@description('Application Insights instrumentation key')
output applicationInsightsKey string = enableApplicationInsights ? appInsights.properties.InstrumentationKey : ''

@description('Application Insights connection string')
output applicationInsightsConnectionString string = enableApplicationInsights ? appInsights.properties.ConnectionString : ''
