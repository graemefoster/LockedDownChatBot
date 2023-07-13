param botName string
param botIdentityName string
param hostName string
param logAnalyticsId string
param appInsightsInstrumentationKey string

param optionalAadTenantId string
param optionalAadClientId string
@secure()
param optionalAadClientSecret string
param optionalAadRequiredScopes string

//Used if you need a Bot based on AAD. Blank if you don't
param localBotAadId string
param localBotAadTenant string
param localBotAadTenantType string

var oauthSignInCardName = 'sample-aad-auth'

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

  resource oauthSignIn 'connections@2022-09-15' = if (optionalAadClientId != '') {
    name: oauthSignInCardName
    kind: 'sdk'
    properties: {
      clientId: optionalAadClientId
      clientSecret: optionalAadClientSecret
      parameters: [
        {
          key: 'tenantId'
          value: optionalAadTenantId
        }
        {
          key: 'scopes'
          value: optionalAadRequiredScopes
        }
      ]
      scopes: optionalAadRequiredScopes
      name: oauthSignInCardName
      serviceProviderId: '30dd229c-58e3-4a48-bdfd-91ec48eb906c' //AAD V2 magic GUID
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
