targetScope = 'resourceGroup'
param existing bool
param openAiResourceName string
param openAiModelName string

resource openAiExisting 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = if (existing) {
  name: openAiResourceName
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' existing = if (existing) {
  parent: openAiExisting
  name: openAiModelName
}

resource openAiNew 'Microsoft.CognitiveServices/accounts@2023-05-01' = if (!existing) {
  name: openAiResourceName
  location: 'eastus' //hardcode for now
  kind: 'OpenAI'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  properties: {
    publicNetworkAccess: 'Disabled'
    customSubDomainName: openAiResourceName
  }
}

resource deploymentNew 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (!existing) {
  name: openAiModelName
  parent: openAiNew
  sku: {
    name: 'Standard'
    capacity: 100
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
}

output id string = existing ? openAiExisting.id : openAiNew.id
output openAiName string = existing ? openAiExisting.name : openAiNew.name
output openAiEndpoint string = existing ? openAiExisting.properties.endpoint : openAiNew.properties.endpoint
output modelName string = existing ? deployment.name : deploymentNew.name
// // output id string = openAiNew.id
// // output openAiName string = openAiNew.name
// // output openAiEndpoint string = openAiNew.properties.endpoint
// // output modelName string = deploymentNew.name
// output id string = 'openAiNew.id'
// output openAiName string = 'openAiNew.name'
// output openAiEndpoint string = 'openAiNew.properties.endpoint'
// output modelName string = 'deploymentNew.name'
