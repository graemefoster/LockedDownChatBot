param vnetName string
param routeTableName string
param location string = resourceGroup().location
param vnetCidr string

resource routeTable 'Microsoft.Network/routeTables@2022-11-01' = {
  name: routeTableName
  location: location
}

resource vnet 'Microsoft.Network/virtualNetworks@2022-11-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [vnetCidr]
    }
    subnets: [
      {
        name: 'AzureFirewallSubnet'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 1)
        }
      }
      {
        name: 'AppServiceDelegated'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 2)
          delegations: [
            {
              name: 'AppServiceDelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
          routeTable: {
            id: routeTable.id
          }
        }
      }
      {
        name: 'PrivateEndpoints'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 3)
          privateEndpointNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'AGSubnet'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 4)
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
          networkSecurityGroup: {
            id: gwayNsg.id
          }
        }
      }
      {
        name: 'AzureFirewallManagementSubnet'
        properties: {
          addressPrefix: cidrSubnet(vnetCidr, 24, 5)
        }
      }
    ]
  }
}

resource gwayNsg 'Microsoft.Network/networkSecurityGroups@2022-11-01' = {
  name: '${vnetName}-gway-nsg'
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowIncomingFromInternetToBot'
        properties: {
          access: 'Allow'
          direction: 'Inbound'
          priority: 1000
          protocol: 'Tcp'
          description: 'Let Bot traffic in'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}


resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurewebsites.net'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.azurewebsites.net-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource cosmosPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.documents.azure.com'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.documents.azure.com-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource openAiPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.openai.azure.com'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.openai.azure.com-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

resource cogSearchPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.search.windows.net'
  location: 'global'

  resource vnetLink 'virtualNetworkLinks@2020-06-01' = {
    name: 'privatelink.search.windows.net-link'
    location: 'global'
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnet.id
      }
    }
  }
}

output firewallSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'AzureFirewallSubnet')[0].id
output firewallManagementSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'AzureFirewallManagementSubnet')[0].id
output privateEndpointSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'PrivateEndpoints')[0].id
output vnetIntegrationSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'AppServiceDelegated')[0].id
output agSubnetId string = filter(vnet.properties.subnets, subnet => subnet.name == 'AGSubnet')[0].id
output privateDnsZoneId string = privateDnsZone.id
output openAiPrivateDnsZoneId string = openAiPrivateDnsZone.id
output cosmosPrivateDnsZoneId string = cosmosPrivateDnsZone.id
output cogSearchPrivateDnsZoneId string = cogSearchPrivateDnsZone.id
