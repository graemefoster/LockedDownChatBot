param openAiResourceId string
param privateEndpointSubnetId string
param privateDnsZoneId string
param location string = resourceGroup().location

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: 'openai-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'openai-private-link-service-connection'
        properties: {
          privateLinkServiceId: openAiResourceId
          groupIds: [
            'account'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: 'openai-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'openai-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}
