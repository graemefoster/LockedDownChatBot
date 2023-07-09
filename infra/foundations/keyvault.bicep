param kvName string
param location string = resourceGroup().location
param kvGroupObjectId string
param logAnalyticsName string

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

output kvName string = kvName
output kvId string = keyvault.id
output kvUri string = keyvault.properties.vaultUri
output logAnalyticsId string = lanalytics.id
output openAiSecretName string = openAiSecretName
