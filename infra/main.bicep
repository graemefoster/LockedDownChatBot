targetScope = 'subscription'

// The main bicep module to provision Azure resources.
// For a more complete walkthrough to understand how this file works with azd,
// see https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-create

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('RG of a public DNS to register domains against')
param dnsResourceGroupName string

@description('Name of a public DNS to register domains against')
param dnsResourceName string

param kvGroupObjectId string

@description('Custom Host name to be added to custom domain suffix')
param chatApiCustomHost string

param openAiResourceId string
param openAiModel string
@secure()
param openAiKey string
param openAiEndpoint string

param localBotAadId string = ''
@description('Required for SingleTenant bot. Else can be empty')
param localBotAadTenant string = ''
@allowed(['MultiTenant', 'SingleTenant'])
param localBotAadTenantType string = 'MultiTenant'


var abbrs = loadJsonContent('./abbreviations.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
}

// Generate a unique token to be used in naming resources.
// Remove linter suppression after using.
#disable-next-line no-unused-vars
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Name of the service defined in azure.yaml
// A tag named azd-service-name with this value should be applied to the service host resource, such as:
//   Microsoft.Web/sites for appservice, function
// Example usage:
//   tags: union(tags, { 'azd-service-name': apiServiceName })
#disable-next-line no-unused-vars
var apiServiceName = 'python-api'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' existing = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
}

//not nice, but I want to get a certificate into KV before running this template
var kvName = rg.tags['env-kv-name']


// Add resources to be provisioned below.
// A full example that leverages azd bicep modules can be seen in the todo-python-mongo template:
// https://github.com/Azure-Samples/todo-python-mongo/tree/main/infra

var routeTableName = '${abbrs.networkRouteTables}internetviafwall'
module vnet 'foundations/vnet.bicep' = {
  name: '${deployment().name}-vnet'
  scope: rg
  params: {
    vnetCidr: '10.0.0.0/12'
    vnetName: '${abbrs.networkVirtualNetworks}vnet'
    location: location
    routeTableName: routeTableName
  }
}

module openAiPrivateEndpoint 'foundations/openai-private-endpoint.bicep' = {
  name: '${deployment().name}-openaipe'
  scope: rg
  params: {
    location: location
    openAiResourceId: openAiResourceId
    privateDnsZoneId: vnet.outputs.openAiPrivateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
  }
}

var gwayName = '${abbrs.networkApplicationGateways}gway'

module managedIdentities 'foundations/identities.bicep' = {
  name: '${deployment().name}-identities'
  scope: rg
  params: {
    gwayName: gwayName
    location: location
  }
}

module core 'foundations/keyvault.bicep' = {
  name: '${deployment().name}-core'
  scope: rg
  params: {
    location: location
    kvGroupObjectId: kvGroupObjectId
    kvName: kvName
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}-${environmentName}-logs'
    openAiKey: openAiKey
    gatewayIdentityId: managedIdentities.outputs.gwayIdentityPrincipalId
    appServiceIdentityId: managedIdentities.outputs.aspIdentityPrincipalId
  }
}

var fwallPolicyName = '${abbrs.networkFirewallPolicies}fwallpcy'

module firewall 'foundations/firewall.bicep' = {
  name: '${deployment().name}-fwall'
  scope: rg
  params: {
    firewallPipName: '${abbrs.networkPublicIPAddresses}fwall'
    firewallMgmtPipName: '${abbrs.networkPublicIPAddresses}fwallmgmt'
    firewallName: '${abbrs.networkAzureFirewalls}botfwall'
    firewallPolicyName: fwallPolicyName
    firewallSubnetId: vnet.outputs.firewallSubnetId
    firewallManagementSubnetId: vnet.outputs.firewallManagementSubnetId
    firewallRouteTableName: routeTableName
    location: location
    logAnalyticsId: core.outputs.logAnalyticsId
  }
}

module database 'foundations/database.bicep' = {
  name: '${deployment().name}-db'
  scope: rg
  params: {
    location: location
    databaseAccountName: '${abbrs.documentDBDatabaseAccounts}botstorage'
    webAppManagedIdentityObjectId: managedIdentities.outputs.aspIdentityPrincipalId
    kvName: kvName
    privateDnsZoneId: vnet.outputs.cosmosPrivateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
  }
}

var appName = '${abbrs.webSitesAppService}${uniqueString(rg.name)}-bot'

module app 'app/bot-app.bicep' = {
  name: '${deployment().name}-app'
  scope: rg
  params: {
    aspName: '${abbrs.webServerFarms}botasp'
    appName: appName
    botCustomHostName: chatApiCustomHost
    location: location
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    vnetIntegrationSubnetId: vnet.outputs.vnetIntegrationSubnetId
    privateDnsZoneId: vnet.outputs.privateDnsZoneId
    tags: tags
    logAnalyticsId: core.outputs.logAnalyticsId
    openAiModel: openAiModel
    openAiEndpoint: openAiEndpoint
    kvName: core.outputs.kvName
    openAiSecretName: core.outputs.openAiSecretName
    appServiceManagedIdentityName: managedIdentities.outputs.aspIdentityName
    cosmosAuthKeySecretName: database.outputs.cosmosSecretName
    cosmosContainerId: database.outputs.containerName
    cosmosDatabaseId: database.outputs.databaseName
    cosmosEndpoint: database.outputs.cosmosUrl
    cosmosConnectionDetails: {
      authKeySecretName: database.outputs.cosmosSecretName
      containerId: database.outputs.containerName
      databaseId: database.outputs.databaseName
      endpoint: database.outputs.cosmosUrl
    }
  }
}

var gatewayCustomHostName = '${chatApiCustomHost}.${dnsResourceName}'

module bot 'bot/bot-service.bicep' = {
  name: '${deployment().name}-bot'
  scope: rg
  params: {
    botName: '${environmentName}-bottestfwall'
    hostName: gatewayCustomHostName
    botIdentityName: app.outputs.botIdentityName
    logAnalyticsId: core.outputs.logAnalyticsId
    appInsightsInstrumentationKey: app.outputs.appInsightsKey
    localBotAadId: localBotAadId
    localBotAadTenant: localBotAadTenant
    localBotAadTenantType: localBotAadTenantType
  }
}

module gateway 'foundations/gateway.bicep' = {
  name: '${deployment().name}-gway'
  scope: rg
  params: {
    chatApiCustomHost: chatApiCustomHost
    cutomSuffix: dnsResourceName
    kvUri: core.outputs.kvUri
    resourceName: gwayName
    managedIdentityId: managedIdentities.outputs.gwayIdentityId
    location: location
    dnsRg: dnsResourceGroupName
    gwaySubnetId: vnet.outputs.agSubnetId
    appServiceAppName: app.outputs.appName
    logAnalyticsId: core.outputs.logAnalyticsId
  }
}

// Add outputs from the deployment here, if needed.
//
// This allows the outputs to be referenced by other bicep deployments in the deployment pipeline,
// or by the local machine as a way to reference created resources in Azure for local development.
// Secrets should not be added here.
//
// Outputs are automatically saved in the local azd environment .env file.
// To see these outputs, run `azd env get-values`,  or `azd env get-values --output json` for json output.
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
