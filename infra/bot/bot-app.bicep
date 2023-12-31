param appName string
param botCustomHostName string
param privateEndpointSubnetId string
param vnetIntegrationSubnetId string
param privateDnsZoneId string
param tags object
param location string = resourceGroup().location
param logAnalyticsId string
param openAiModel string
param openAiEmbeddingModel string
param openAiEndpoint string
param kvName string
param openAiSecretName string
param appServiceManagedIdentityName string
param cosmosAuthKeySecretName string
param cosmosEndpoint string
param cosmosDatabaseId string
param cosmosContainerId string
param applicationInsightsConnectionString string
param aspId string
param apiUrl string
param searchEndpointUrl string
param searchIndexName string

//If set to true then lock the web-app down to private endpoint
param deployEdgeSecurity bool

// EXPERIMENTAL - BREAKS DEPLOYMENT :(
// //Unclear if Bot Composer supports Managed Identity connections. Stored in KV for now
// type cosmosConnectionDetailsType = {
//   authKeySecretName: string
//   endpoint: string
//   databaseId: string
//   containerId: string
// }

// param cosmosConnectionDetails cosmosConnectionDetailsType

resource botIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: appServiceManagedIdentityName
}

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  tags: union(tags, { 'azd-service-name': 'locked-down-bot' })
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${botIdentity.id}': {}
    }
  }
  properties: {
    httpsOnly: true
    serverFarmId: aspId
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    publicNetworkAccess: 'Enabled' //simulate locked-down network by blocking access to app site. But I need to deploy, so I open up SCM site.
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: botIdentity.id
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictionsDefaultAction: deployEdgeSecurity ? 'Deny' : 'Allow' //set to Deny to simulate locked down environment if we are deploying edge gateway
      scmIpSecurityRestrictionsDefaultAction: 'Allow'
      ipSecurityRestrictions: []
      scmIpSecurityRestrictions: []
      cors: {
        allowedOrigins: [
          'https://portal.azure.com' //enables webchat in Azure portal
        ]
      }
      appSettings: [
        {
          name: 'MicrosoftAppTenantId'
          value: botIdentity.properties.tenantId
        }
        {
          name: 'MicrosoftAppId'
          value: botIdentity.properties.clientId
        }
        {
          name: 'MicrosoftAppType'
          value: 'UserAssignedMSI'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ACCOUNTS_BASE_URL'
          value: apiUrl
        }
        {
          name: 'OPENAI_MODEL'
          value: openAiModel
        }
        {
          name: 'OPENAI_EMBEDDING_MODEL'
          value: openAiEmbeddingModel
        }
        {
          name: 'OPENAI_ENDPOINT'
          value: openAiEndpoint
        }
        {
          name: 'OPENAI_KEY'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${openAiSecretName})'
        }
        {
          name: 'runtimeSettings__storage'
          value: 'CosmosDbPartitionedStorage'
        }
        {
          name: 'CosmosDbPartitionedStorage__authKey'
          value: '@Microsoft.KeyVault(VaultName=${kvName};SecretName=${cosmosAuthKeySecretName})'
        }
        {
          name: 'CosmosDbPartitionedStorage__cosmosDBEndpoint'
          value: cosmosEndpoint
        }
        {
          name: 'CosmosDbPartitionedStorage__databaseId'
          value: cosmosDatabaseId
        }
        {
          name: 'CosmosDbPartitionedStorage__containerId'
          value: cosmosContainerId
        }
        {
          name: 'AzureBotTokenService_AUTHENTICATION_SECRET'
          value: 'IGNORE-THIS-IS-NEVER-USED'
        }
        {
          name: 'COGNITIVE_SEARCH_URL'
          value: searchEndpointUrl
        }
        {
          name: 'COGNITIVE_SEARCH_INDEX'
          value: searchIndexName
        }
        {
          name: 'COGNITIVE_SEARCH_MANAGED_IDENTITY_CLIENT_ID'
          value: botIdentity.properties.clientId
        }
        {
          name: 'OPENAI_MANAGED_IDENTITY_CLIENT_ID'
          value: botIdentity.properties.clientId
        }
        {
          name: 'BOT_MEMORY_HOST'
          value: cosmosEndpoint
        }
      ]
    }
  }

  resource auth 'config@2022-09-01' = {
    name: 'authsettingsV2'
    properties: {
      globalValidation: {
        unauthenticatedClientAction: 'Return401'
        requireAuthentication: true
      }
      identityProviders: {
        customOpenIdConnectProviders: {
          AzureBotTokenService: {
            registration: {
              clientCredential: {
                clientSecretSettingName: 'AzureBotTokenService_AUTHENTICATION_SECRET'
              }
              clientId: botIdentity.properties.clientId
              openIdConnectConfiguration: {
                wellKnownOpenIdConfiguration: 'https://login.botframework.com/v1/.well-known/openidconfiguration'
              }
            }
          }
        }
      }
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
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
      {
        category: 'AppServiceAuditLogs'
        enabled: true
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
      }
    ]
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = if (deployEdgeSecurity) {
  name: '${botCustomHostName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${botCustomHostName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: app.id
          groupIds: [
            'sites'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = if (deployEdgeSecurity) {
    name: '${botCustomHostName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${botCustomHostName}-private-endpoint-cfg'
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
output defaultHostName string = app.properties.defaultHostName

