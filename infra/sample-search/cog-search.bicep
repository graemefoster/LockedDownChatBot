//sample cognitive search index which we can search over from open-ai
param cogServicesSearchName string
param cogSearchDnsZoneId string
param privateEndpointSubnetId string
param logAnalyticsId string
param appServiceIdentityPrincipalId string
param location string = resourceGroup().location

resource cogSearch 'Microsoft.Search/searchServices@2022-09-01' = {
  name: cogServicesSearchName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'basic'
  }
  properties: {
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http403'
      }
    }
    hostingMode: 'default'
  }

}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: cogSearch
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'OperationLogs'
        enabled: true
      }
    ]
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: '${cogServicesSearchName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${cogServicesSearchName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: cogSearch.id
          groupIds: [
            'searchService'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: '${cogServicesSearchName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${cogServicesSearchName}-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: cogSearchDnsZoneId
          }
        }
      ]
    }
  }
}

resource searchRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
}

resource appServiceRbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${appServiceIdentityPrincipalId}-search-${cogSearch.id}')
  scope: cogSearch
  properties: {
    roleDefinitionId: searchRole.id
    principalId: appServiceIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

var storageName = substring(replace(replace(toLower(cogServicesSearchName), '-', ''), '_', ''), 0, 23)

resource searchStorage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: true
  }
}


var containerName = 'sample-documents'
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  name: 'default'
  parent: searchStorage

  resource container 'containers@2022-09-01' = {
    name: containerName
    properties: {
      publicAccess: 'None'
    }
  }
}

output searchEndpoint string = 'https://${cogSearch.name}.search.windows.net'
output storageName string = storageName
output containerName string = containerName
output indexName string = 'info-idx'
