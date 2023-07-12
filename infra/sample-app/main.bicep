param aspId string
param privateEndpointSubnetId string
param vnetIntegrationSubnetId string
param privateDnsZoneId string
param sampleAppName string
param appInsightsConnectionString string
param logAnalyticsId string
param location string = resourceGroup().location

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: sampleAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: aspId
    vnetRouteAllEnabled: true
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    publicNetworkAccess: 'Enabled' //simulate locked-down network by blocking access to app site. But I need to deploy, so I open up SCM site.
    clientAffinityEnabled: false
    siteConfig: {
      minTlsVersion: '1.2'
      alwaysOn: true
      vnetRouteAllEnabled: true
      ipSecurityRestrictionsDefaultAction: 'Deny'
      scmIpSecurityRestrictionsDefaultAction: 'Allow'
      ipSecurityRestrictions: []
      scmIpSecurityRestrictions: []
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'InstrumentationEngine_EXTENSION_VERSION'
          value: '~1'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_BaseExtensions'
          value: '~1'
        }
      ]
    }
  }

}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: app
  name: 'diagnostics'
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
      {
        category: 'AppServiceAuditLogs'
        enabled: true
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
      }
    ]
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-11-01' = {
  name: '${sampleAppName}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${sampleAppName}-private-link-service-connection'
        properties: {
          privateLinkServiceId: app.id
          groupIds: [
            'sites'
          ]
        }
      }
    ]
  }

  resource dnsGroup 'privateDnsZoneGroups@2022-11-01' = {
    name: '${sampleAppName}-private-endpoint-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: '${sampleAppName}-private-endpoint-cfg'
          properties: {
            privateDnsZoneId: privateDnsZoneId
          }
        }
      ]
    }
  }
}

output appUrl string = 'https://${app.properties.defaultHostName}/'
