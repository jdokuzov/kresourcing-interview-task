// Everything the API needs in Azure, except MongoDB and the container registry that holds its image.
// The steps to deploy this are in the README.

@description('Prefix for resource names.')
param appName string = 'claims'

param location string = resourceGroup().location

@description('Name of an existing container registry in this resource group.')
param registryName string

@description('Image tag in the registry, built from the Dockerfile.')
param imageTag string

@description('Connection string of a MongoDB deployment that supports transactions.')
@secure()
param mongoConnectionString string

@description('The API has no authentication, so it is only reachable from inside the environment unless this is true.')
param publicIngress bool = true

var uniqueSuffix = uniqueString(resourceGroup().id)
var auditQueueName = 'audit-events' // Must match AuditConsumer.QueueName
var auditDatabaseName = 'audit'

// Built-in Azure role ids
var acrPullRole = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var serviceBusSenderRole = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
var serviceBusReceiverRole = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'

// The application signs in to the registry, Service Bus and SQL with this identity, so they need no passwords.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${appName}-identity'
  location: location
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

resource registryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRole)
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRole)
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${appName}-${uniqueSuffix}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    disableLocalAuth: true
  }
}

resource auditQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: auditQueueName
  properties: {
    maxDeliveryCount: 10
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
  }
}

resource queueSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(auditQueue.id, identity.id, serviceBusSenderRole)
  scope: auditQueue
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusSenderRole)
  }
}

resource queueReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(auditQueue.id, identity.id, serviceBusReceiverRole)
  scope: auditQueue
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusReceiverRole)
  }
}

// The identity is the server's administrator, because the application runs its own migrations on startup.
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${appName}-${uniqueSuffix}'
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      principalType: 'Application'
      login: identity.name
      sid: identity.properties.principalId
      tenantId: subscription().tenantId
    }
  }
}

resource auditDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: auditDatabaseName
  location: location
  sku: {
    name: 'Basic'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: publicIngress
        targetPort: 8080
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        {
          name: 'mongo-connection-string'
          value: mongoConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${registry.properties.loginServer}/claims-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'AZURE_CLIENT_ID'
              value: identity.properties.clientId
            }
            {
              name: 'ConnectionStrings__AuditDatabase'
              value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${auditDatabaseName};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True'
            }
            {
              name: 'ConnectionStrings__ClaimsDatabase'
              secretRef: 'mongo-connection-string'
            }
            {
              name: 'ServiceBus__FullyQualifiedNamespace'
              value: '${serviceBus.name}.servicebus.windows.net'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
            }
          ]
        }
      ]
      // The outbox relay and the audit consumer run inside the API, so it must never scale to zero.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    registryPull
    queueSender
    queueReceiver
    auditDatabase
    allowAzureServices
  ]
}

output apiHost string = app.properties.configuration.ingress.fqdn
