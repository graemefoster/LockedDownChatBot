param botName string
param botIdentityName string
param hostName string
param logAnalyticsId string
param appInsightsInstrumentationKey string

//Used if you need a Bot based on AAD. Blank if you don't
param localBotAadId string
param localBotAadTenant string
param localBotAadTenantType string

resource botIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: botIdentityName
}

resource botForLocalDev 'Microsoft.BotService/botServices@2022-09-15' = if (localBotAadId != '') {
  name: '${botName}-local'
  location: 'global'
  kind: 'sdk'
  properties: {
    displayName: botName
    msaAppType: localBotAadTenantType
    msaAppId: localBotAadId
    msaAppTenantId: localBotAadTenantType == 'MultiTenant' ? null : localBotAadTenant
    endpoint: 'https://${hostName}/api/messages'
    developerAppInsightKey: appInsightsInstrumentationKey
  }

  resource teamsChannel 'channels@2022-09-15' = {
    name: 'MsTeamsChannel'
    location: 'global'
    kind: 'sdk'
    properties: {
      channelName: 'MsTeamsChannel'
    }
  }
}

resource bot 'Microsoft.BotService/botServices@2022-09-15' = {
  name: botName
  location: 'global'
  kind: 'sdk'
  properties: {
    displayName: botName
    msaAppType: 'UserAssignedMSI'
    msaAppMSIResourceId: botIdentity.id
    msaAppId: botIdentity.properties.clientId
    msaAppTenantId: botIdentity.properties.tenantId
    endpoint: 'https://${hostName}/api/messages'
    developerAppInsightKey: appInsightsInstrumentationKey
  }

  resource teamsChannel 'channels@2022-09-15' = {
    name: 'MsTeamsChannel'
    location: 'global'
    kind: 'sdk'
    properties: {
      channelName: 'MsTeamsChannel'
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: bot
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
