param databaseAccountName string
param location string = resourceGroup().location
param webAppManagedIdentityObjectId string
param kvName string
param privateEndpointSubnetId string
param privateDnsZoneId string

resource database 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: databaseAccountName
  kind: 'GlobalDocumentDB'
  location: location
  properties: {
    locations: [ {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      } ]
    capacity: {
      totalThroughputLimit: 4000
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    databaseAccountOfferType: 'Standard'
  }

}

var databaseName = 'bot-db'
resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: database
  name: databaseName
  location: location
  properties: {
    resource: {
      id: databaseName
    }
  }
}

var sqlContainerName = 'bot-container'
resource sqlContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: db
  name: sqlContainerName
  location: location
  properties: {
    resource: {
      id: sqlContainerName
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: '${databaseName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${databaseName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: database.id
          groupIds: [
            'Sql'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: '${databaseName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${databaseName}-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}

//allow web-app to connect via its managed identity
//https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac
resource dataContributorRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2023-04-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: database
}

//Doesn't use the normal RoleAssignment - this one is specific to Cosmos.
resource webAppRbac 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid('${webAppManagedIdentityObjectId}-${database.id}')
  properties: {
    principalId: webAppManagedIdentityObjectId
    roleDefinitionId: dataContributorRole.id
    scope: database.id
  }
}

//put the auth key into KV
var authKeySecretName = 'DatabaseAuthKey'
resource kv 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: kvName
  resource secret 'secrets@2023-02-01' = {
    name: authKeySecretName
    properties: {
      value: database.listKeys().primaryMasterKey
    }
  }
}

output cosmosId string = database.id
output cosmosSecretName string = authKeySecretName
output cosmosUrl string = database.properties.documentEndpoint
output databaseName string = databaseName
output containerName string = sqlContainerName
