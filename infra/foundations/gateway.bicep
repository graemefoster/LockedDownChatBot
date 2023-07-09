param resourceName string
param location string = resourceGroup().location
param kvUri string
param chatApiCustomHost string
param cutomSuffix string
param dnsRg string
param gwaySubnetId string
param managedIdentityId string
param appServiceAppName string
param logAnalyticsId string

resource appGwayPip 'Microsoft.Network/publicIPAddresses@2022-11-01' = {
  name: '${resourceName}-pip'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
  zones: pickZones('Microsoft.Network', 'publicIPAddresses', location, 3)
}

resource appgway 'Microsoft.Network/applicationGateways@2022-11-01' = {
  name: resourceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    sku: {
      name: 'Standard_v2'
      tier: 'Standard_v2'
    }
    gatewayIPConfigurations: [
      {
        name: 'ipconfig'
        properties: {
          subnet: {
            id: gwaySubnetId
          }
        }
      }
    ]
    frontendIPConfigurations: [
      {
        name: 'frontend-ip'
        properties: {
          publicIPAddress: {
            id: appGwayPip.id
          }
        }
      }
    ]
    frontendPorts: [
      {
        name: 'https'
        properties: {
          port: 443
        }
      }
    ]
    sslCertificates: [
      {
        name: 'tlscert'
        properties: {
          keyVaultSecretId: '${kvUri}secrets/${chatApiCustomHost}'
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'appservice'
        properties: {
          backendAddresses: [
            {
              fqdn: '${appServiceAppName}.azurewebsites.net'
            }
          ]
        }
      }
    ]
    backendHttpSettingsCollection: [
      {
        name: 'passthrutoappservice'
        properties: {
          protocol: 'Https'
          port: 443
          probeEnabled: true
          hostName: '${appServiceAppName}.azurewebsites.net'
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', resourceName, 'app-probe')
          }
        }
      }
      {
        name: 'scmsite'
        properties: {
          protocol: 'Https'
          port: 443
          probeEnabled: true
          hostName: '${appServiceAppName}.scm.azurewebsites.net'
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', resourceName, 'scm-probe')
          }
        }
      }
    ]
    probes: [
      {
        name: 'app-probe'
        properties: {
          pickHostNameFromBackendHttpSettings: true
          path: '/'
          port: 443
          timeout: 30
          interval: 600
          unhealthyThreshold: 3
          protocol: 'Https'
          match: {
            statusCodes: [ '200-499' ]
          }
        }
      }
      {
        name: 'scm-probe'
        properties: {
          pickHostNameFromBackendHttpSettings: true
          path: '/'
          port: 443
          timeout: 30
          interval: 600
          unhealthyThreshold: 3
          protocol: 'Https'
          match: {
            statusCodes: [ '200-499' ]
          }
        }
      }
    ]
    httpListeners: [
      {
        name: 'https-listener'
        properties: {
          protocol: 'Https'
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', resourceName, 'frontend-ip')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', resourceName, 'https')
          }
          sslCertificate: {
            id: resourceId('Microsoft.Network/applicationGateways/sslCertificates', resourceName, 'tlscert')
          }
          hostNames: [
            '${chatApiCustomHost}.${cutomSuffix}'
          ]
        }
      }
      {
        name: 'https-scm-listener'
        properties: {
          protocol: 'Https'
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', resourceName, 'frontend-ip')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', resourceName, 'https')
          }
          sslCertificate: {
            id: resourceId('Microsoft.Network/applicationGateways/sslCertificates', resourceName, 'tlscert')
          }
          hostNames: [
            '${chatApiCustomHost}.scm.${cutomSuffix}'
          ]
        }
      }
    ]
    requestRoutingRules: [
      {
        name: 'appserviceroute'
        properties: {
          ruleType: 'Basic'
          priority: 100
          backendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', resourceName, 'passthrutoappservice')
          }
          backendAddressPool: {
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', resourceName, 'appservice')
          }
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', resourceName, 'https-listener')
          }
        }
      }
      {
        name: 'appservicescmroute'
        properties: {
          ruleType: 'Basic'
          priority: 101
          backendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', resourceName, 'scmsite')
          }
          backendAddressPool: {
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', resourceName, 'appservice')
          }
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', resourceName, 'https-scm-listener')
          }
        }
      }
    ]
    autoscaleConfiguration: {
      minCapacity: 0
      maxCapacity: 2
    }
  }
}



resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: appgway
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


module dnsToGatewayPublicIp 'gateway-public-dns.bicep' = {
  scope: resourceGroup(dnsRg)
  name: '${deployment().name}-gway-a'
  params: {
    dnsResourceName: cutomSuffix
    applicationName: chatApiCustomHost
    gatewayPublicIp: appGwayPip.properties.ipAddress
  }
}
