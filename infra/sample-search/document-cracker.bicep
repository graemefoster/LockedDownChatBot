param functionAppName string
param privateEndpointSubnetId string
param vnetIntegrationSubnetId string
param privateDnsZoneId string
param tags object
param logAnalyticsId string
param appServiceManagedIdentityName string
param aspId string
param kvName string
param storageConnectionStringSecretName string
param appInsightsConnectionString string
param openAiHostName string
param openAiEmbeddingModelName string

param location string = resourceGroup().location

resource botIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: appServiceManagedIdentityName
}

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'document-cracker' })
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${botIdentity.id}': {}
    }
  }
  kind: 'functionapp'
  properties: {
    httpsOnly: true
    serverFarmId: aspId
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: botIdentity.id
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      ipSecurityRestrictionsDefaultAction: 'Deny'
      scmIpSecurityRestrictionsDefaultAction: 'Allow'
      ipSecurityRestrictions: []
      scmIpSecurityRestrictions: []
      vnetRouteAllEnabled: true
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${storageConnectionStringSecretName})'
        }
        {
          name: 'BlobStorageAccount'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${storageConnectionStringSecretName})'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'AzureOpenAIHost'
          value: openAiHostName
        }
        {
          name: 'AzureOpenAIEmbeddingModel'
          value: openAiEmbeddingModelName
        }
        {
          name: 'AzureOpenAIIdentityClientId'
          value: botIdentity.properties.clientId
        }
      ]
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: app
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
      }
    ]
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: '${functionAppName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${functionAppName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: app.id
          groupIds: [
            'sites'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: '${functionAppName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${functionAppName}-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}

output botIdentityName string = botIdentity.name
output appName string = app.name
