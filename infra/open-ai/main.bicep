targetScope = 'resourceGroup'
param existing bool
param openAiResourceName string
param openAiModelName string
param openAiEmbeddingModelName string
param managedIdentityPrincipalId string

resource openAiExisting 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = if (existing) {
  name: openAiResourceName
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' existing = if (existing) {
  parent: openAiExisting
  name: openAiModelName
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' existing = if (existing) {
  parent: openAiExisting
  name: openAiEmbeddingModelName
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
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
}

resource embeddingDeploymentNew 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (!existing) {
  name: openAiEmbeddingModelName
  parent: openAiNew
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  dependsOn: [
    deploymentNew
  ]
}

resource openAiRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource rbacModelReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${managedIdentityPrincipalId}-search-${existing ? openAiExisting.id : openAiNew.id}')
  scope: existing ? openAiExisting : openAiNew
  properties: {
    roleDefinitionId: openAiRole.id
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output id string = existing ? openAiExisting.id : openAiNew.id
output openAiName string = existing ? openAiExisting.name : openAiNew.name
output openAiEndpoint string = existing ? openAiExisting.properties.endpoint : openAiNew.properties.endpoint
output modelName string = existing ? deployment.name : deploymentNew.name
output embeddingModelName string = existing ? embeddingDeployment.name : embeddingDeploymentNew.name
