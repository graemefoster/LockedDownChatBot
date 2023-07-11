param aspName string
param appName string
param botCustomHostName string
param privateEndpointSubnetId string
param vnetIntegrationSubnetId string
param privateDnsZoneId string
param tags object
param location string = resourceGroup().location
param logAnalyticsId string
param openAiModel string
param openAiEndpoint string
param kvName string
param openAiSecretName string
param appServiceManagedIdentityName string
param cosmosAuthKeySecretName string
param cosmosEndpoint string
param cosmosDatabaseId string
param cosmosContainerId string

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

resource asp 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: aspName
  location: location
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
    zoneRedundant: false
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  location: location
  name: '${appName}-appinsights'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'BlueField'
    WorkspaceResourceId: logAnalyticsId
    RetentionInDays: 30
  }
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
    serverFarmId: asp.id
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    publicNetworkAccess: 'Enabled' //simulate locked-down network by blocking access to app site. But I need to deploy, so I open up SCM site.
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: botIdentity.id
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictionsDefaultAction: 'Deny'
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
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'OPENAI_MODEL'
          value: openAiModel
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

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
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

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
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
output appInsightsKey string = appInsights.properties.InstrumentationKey
