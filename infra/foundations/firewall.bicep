param firewallPipName string
param firewallMgmtPipName string
param firewallName string
param firewallPolicyName string
param firewallRouteTableName string
param location string = resourceGroup().location
param firewallSubnetId string
param firewallManagementSubnetId string
param logAnalyticsId string

resource firewallPip 'Microsoft.Network/publicIPAddresses@2022-11-01' = {
  name: firewallPipName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
  zones: pickZones('Microsoft.Network', 'publicIPAddresses', location, 3)
}

resource firewallManagementPip 'Microsoft.Network/publicIPAddresses@2022-11-01' = {
  name: firewallMgmtPipName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
  zones: pickZones('Microsoft.Network', 'publicIPAddresses', location, 3)
}

resource fwallPolicy 'Microsoft.Network/firewallPolicies@2022-11-01' = {
  name: firewallPolicyName
  location: location
  properties: {
    sku: {
      tier: 'Basic'
    }
  }
}

resource firewall 'Microsoft.Network/azureFirewalls@2022-11-01' = {
  name: firewallName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig'
        properties: {
          subnet: {
            id: firewallSubnetId
          }
          publicIPAddress: {
            id: firewallPip.id
          }
        }
      }
    ]
    sku: {
      name: 'AZFW_VNet'
      tier: 'Basic'
    }
    managementIpConfiguration: {
      name: 'mgmntipconfig'
      properties: {
        publicIPAddress: {
          id: firewallManagementPip.id
        }
        subnet: {
          id: firewallManagementSubnetId
        }
      }
    }
    firewallPolicy: {
      id: fwallPolicy.id
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: firewall
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource policyRuleGroup 'Microsoft.Network/firewallPolicies/ruleGroups@2020-04-01' = {
  parent: fwallPolicy
  name: 'defaultRuleGroup'
  properties: {
    priority: 100
    rules: [
      {
        ruleType: 'FirewallPolicyFilterRule'
        action: {
          type: 'Allow'
        }
        name: 'AllowAppServiceOut'
        priority: 100
        ruleConditions: [
          {
            ruleConditionType: 'ApplicationRuleCondition'
            description: 'AppService Outbound requests'
            name: 'AppServiceOut'
            sourceAddresses: [ '*' ]
            protocols: [
              {
                port: 443
                protocolType: 'Https'
              }
              {
                port: 80
                protocolType: 'Http'
              }
            ]
            targetFqdns: [
              '*'
            ]
          }
        ]
      }
    ]
  }
}

resource routeTable 'Microsoft.Network/routeTables@2022-11-01' existing = {
  name: firewallRouteTableName
}

resource firewallRoute 'Microsoft.Network/routeTables/routes@2022-11-01' = {
  parent: routeTable
  name: 'InternetViaFirewall'
  properties: {
    nextHopType: 'VirtualAppliance'
    addressPrefix: '0.0.0.0/0'
    nextHopIpAddress: firewall.properties.ipConfigurations[0].properties.privateIPAddress
  }
}

output publicIpV4 string = firewallPip.properties.ipAddress
