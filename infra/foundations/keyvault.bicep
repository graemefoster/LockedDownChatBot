param kvName string
param location string = resourceGroup().location
param kvGroupObjectId string
param logAnalyticsName string
param aspName string
param appName string

@secure()
param openAiKey string

param gatewayIdentityId string
param appServiceIdentityId string

var openAiSecretName = 'OpenAiKey'

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

//used to store LetsEncrypt certificate we generate on post-hook
resource keyvault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  location: location
  name: kvName
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        objectId: kvGroupObjectId
        tenantId: subscription().tenantId
        permissions: {
          certificates: [ 'all' ]
          keys: [ 'all' ]
          secrets: [ 'all' ]
          storage: [ 'all' ]
        }
      }
      {
        objectId: gatewayIdentityId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get']
        }
      }
      {
        objectId: appServiceIdentityId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
  resource openAiSecret 'secrets@2023-02-01' = {
    name: openAiSecretName
    properties: {
      value: openAiKey
      contentType: 'text/plain'
    }
  }
}

resource kvDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyvault
  name: 'diagnostics'
  properties: {
    workspaceId: lanalytics.id
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
  }
}


resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  location: location
  name: '${appName}-appinsights'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'BlueField'
    WorkspaceResourceId: lanalytics.id
    RetentionInDays: 30
  }
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


output kvName string = kvName
output kvId string = keyvault.id
output kvUri string = keyvault.properties.vaultUri
output logAnalyticsId string = lanalytics.id
output openAiSecretName string = openAiSecretName
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString
output applicationInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output aspId string = asp.id
