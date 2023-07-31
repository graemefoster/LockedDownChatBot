param kvName string
param location string = resourceGroup().location
param logAnalyticsName string

param gatewayIdentityId string
param appServiceIdentityId string

param openAiRg string
param openAiName string


var openAiSecretName = 'OpenAiKey'

resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  scope: resourceGroup(openAiRg)
  name: openAiName
}

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
      value: openAi.listKeys().key1
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
