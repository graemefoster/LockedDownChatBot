param cogServicesSearchName string
param storageDnsZoneId string
param privateEndpointSubnetId string
param appServiceIdentityPrincipalId string
param kvName string
param location string = resourceGroup().location

var storageNameMaybe = replace(replace(toLower(cogServicesSearchName), '-', ''), '_', '')
var storageName = substring(storageNameMaybe, 0, max(length(storageNameMaybe), 23))

resource searchStorage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
  }
}

var containerInName = 'documents-in'
var containerOutName = 'sample-documents'

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  name: 'default'
  parent: searchStorage

  resource containerIn 'containers@2022-09-01' = {
    name: containerInName
    properties: {
      publicAccess: 'None' //allow you to put documents in from the portal
    }
  }

  resource containerOut 'containers@2022-09-01' = {
    name: containerOutName
    properties: {
      publicAccess: 'None'
    }
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: '${storageName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${storageName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: searchStorage.id
          groupIds: [
            'blob'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: '${storageName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${storageName}-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: storageDnsZoneId
          }
        }
      ]
    }
  }
}

resource dataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource appServiceRbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${appServiceIdentityPrincipalId}-search-${searchStorage.id}')
  scope: searchStorage
  properties: {
    roleDefinitionId: dataContributorRole.id
    principalId: appServiceIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource kv 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: kvName
}

var storageSecretName = 'storageConnectionString'
resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  name: storageSecretName
  parent: kv
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${searchStorage.name};AccountKey=${searchStorage.listKeys().keys[0].value}'
  }
}

output storageAccountName string = searchStorage.name
output storageAccountSecretName string = storageSecretName
output documentsToIndexContainerName string = containerOutName
output rawDocumentsContainerName string = containerInName
