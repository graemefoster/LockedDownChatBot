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
@allowed([ 'MultiTenant', 'SingleTenant', '' ])
param localBotAadTenantType string = 'MultiTenant'

//Used to demonstrate getting a JWT from a user
param optionalAadTenantId string
param optionalAadClientId string
@secure()
param optionalAadClientSecret string
param optionalAadRequiredScopes string

//When set to true, deploys Basic Firewall and Application Gateway
param deployEdgeSecurity bool = false

var abbrs = loadJsonContent('./abbreviations.json')

var resourceNameSuffix = toLower(replace(environmentName, '-', ''))

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
}

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' existing = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
}

//not nice, but I want to get a certificate into KV before running this template
var kvName = rg.tags['env-kv-name']

var appName = '${abbrs.webSitesAppService}${resourceNameSuffix}-bot'
var functionName = '${abbrs.webSitesAppService}${resourceNameSuffix}-crkr'
var searchServicesAccountName = '${abbrs.searchSearchServices}${resourceNameSuffix}-bot'

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

var gwayName = '${abbrs.networkApplicationGateways}${resourceNameSuffix}'

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
    kvName: kvName
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}-${environmentName}-logs'
    openAiKey: openAiKey
    gatewayIdentityId: managedIdentities.outputs.gwayIdentityPrincipalId
    appServiceIdentityId: managedIdentities.outputs.aspIdentityPrincipalId
    appName: appName
    aspName: '${abbrs.webServerFarms}${resourceNameSuffix}'
  }
}

var fwallPolicyName = '${abbrs.networkFirewallPolicies}${resourceNameSuffix}'

module firewall 'foundations/firewall.bicep' = if (deployEdgeSecurity) {
  name: '${deployment().name}-fwall'
  scope: rg
  params: {
    firewallPipName: '${abbrs.networkPublicIPAddresses}${resourceNameSuffix}-fwall'
    firewallMgmtPipName: '${abbrs.networkPublicIPAddresses}${resourceNameSuffix}-fwallmgmt'
    firewallName: '${abbrs.networkAzureFirewalls}${resourceNameSuffix}-botfwall'
    firewallPolicyName: fwallPolicyName
    firewallSubnetId: vnet.outputs.firewallSubnetId
    firewallManagementSubnetId: vnet.outputs.firewallManagementSubnetId
    firewallRouteTableName: routeTableName
    location: location
    logAnalyticsId: core.outputs.logAnalyticsId
  }
}

module storage 'foundations/storage.bicep' = {
  name: '${deployment().name}-stg'
  scope: rg
  params: {
    appServiceIdentityPrincipalId: managedIdentities.outputs.aspIdentityPrincipalId
    cogServicesSearchName: searchServicesAccountName
    kvName: core.outputs.kvName
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    storageDnsZoneId: vnet.outputs.storagePrivateDnsZoneId
    location: location
  }
}

module database 'foundations/database.bicep' = {
  name: '${deployment().name}-db'
  scope: rg
  params: {
    location: location
    databaseAccountName: '${abbrs.documentDBDatabaseAccounts}${resourceNameSuffix}-bot'
    webAppManagedIdentityObjectId: managedIdentities.outputs.aspIdentityPrincipalId
    kvName: kvName
    privateDnsZoneId: vnet.outputs.cosmosPrivateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
  }
}

var sampleApiName = '${abbrs.webSitesAppService}${resourceNameSuffix}-api'
module sampleApp 'sample-app/main.bicep' = {
  name: '${deployment().name}-sampleapi'
  scope: rg
  params: {
    logAnalyticsId: core.outputs.logAnalyticsId
    appInsightsConnectionString: core.outputs.applicationInsightsConnectionString
    aspId: core.outputs.aspId
    privateDnsZoneId: vnet.outputs.privateDnsZoneId
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    sampleAppName: sampleApiName
    vnetIntegrationSubnetId: vnet.outputs.vnetIntegrationSubnetId
    location: location
  }
}


module app 'bot/bot-app.bicep' = {
  name: '${deployment().name}-app'
  scope: rg
  params: {
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
    applicationInsightsConnectionString: core.outputs.applicationInsightsConnectionString
    aspId: core.outputs.aspId
    apiUrl: sampleApp.outputs.appUrl
    deployEdgeSecurity: deployEdgeSecurity
    searchEndpointUrl: cogSearchIndex.outputs.searchEndpoint
    searchIndexName: cogSearchIndex.outputs.indexName
  }
}

module documentCracker 'sample-search/document-cracker.bicep' = {
  name: '${deployment().name}-cracker'
  scope: rg
  params: {
    functionAppName: functionName
    location: location
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    vnetIntegrationSubnetId: vnet.outputs.vnetIntegrationSubnetId
    privateDnsZoneId: vnet.outputs.privateDnsZoneId
    tags: tags
    logAnalyticsId: core.outputs.logAnalyticsId
    kvName: core.outputs.kvName
    appServiceManagedIdentityName: managedIdentities.outputs.aspIdentityName
    aspId: core.outputs.aspId
    storageConnectionStringSecretName: storage.outputs.storageAccountSecretName
    appInsightsConnectionString: core.outputs.applicationInsightsConnectionString
  }
}

var gatewayCustomHostName = '${chatApiCustomHost}.${dnsResourceName}'

module bot 'bot/bot-service.bicep' = {
  name: '${deployment().name}-bot'
  scope: rg
  params: {
    botName: '${environmentName}-bot'
    hostName: deployEdgeSecurity ? gatewayCustomHostName : app.outputs.defaultHostName
    botIdentityName: app.outputs.botIdentityName
    logAnalyticsId: core.outputs.logAnalyticsId
    appInsightsInstrumentationKey: core.outputs.applicationInsightsInstrumentationKey
    localBotAadId: localBotAadId
    localBotAadTenant: localBotAadTenant
    localBotAadTenantType: localBotAadTenantType
    optionalAadTenantId: optionalAadTenantId
    optionalAadClientId: optionalAadClientId
    optionalAadClientSecret: optionalAadClientSecret
    optionalAadRequiredScopes: optionalAadRequiredScopes
  }
}

module gateway 'foundations/gateway.bicep' = if (deployEdgeSecurity) {
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

module cogSearchIndex 'sample-search/cog-search.bicep' = {
  name: '${deployment().name}-cogsearch'
  scope: rg
  params: {
    location: location
    cogSearchDnsZoneId: vnet.outputs.cogSearchPrivateDnsZoneId
    cogServicesSearchName: searchServicesAccountName
    privateEndpointSubnetId: vnet.outputs.privateEndpointSubnetId
    logAnalyticsId: core.outputs.logAnalyticsId
    appServiceIdentityPrincipalId: managedIdentities.outputs.aspIdentityPrincipalId
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

output OUT_AZURE_SEARCH_ENDPOINT string = cogSearchIndex.outputs.searchEndpoint
output OUT_AZURE_SEARCH_INDEX_NAME string = cogSearchIndex.outputs.indexName
output OUT_AZURE_STORAGE_ACCOUNT_NAME string = storage.outputs.storageAccountName
output OUT_AZURE_STORAGE_CONTAINER_NAME string = storage.outputs.documentsToIndexContainerName
output OUT_AZURE_RESOURCE_GROUP string = rg.name
