param gwayName string
param location string = resourceGroup().location

resource gwayIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  location: location
  name: '${gwayName}-identity'
}

resource appServiceIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  location: location
  name: 'appsvc-identity'
}

output gwayIdentityId string = gwayIdentity.id
output gwayIdentityName string = gwayIdentity.name
output gwayIdentityPrincipalId string = gwayIdentity.properties.principalId

output aspIdentityId string = appServiceIdentity.id
output aspIdentityName string = appServiceIdentity.name
output aspIdentityPrincipalId string = appServiceIdentity.properties.principalId
