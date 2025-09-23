// Bicep file for FurnitureFinder POC Azure resources
param location string = 'eastus'
param resourcePrefix string = 'ffpoc'

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${resourcePrefix}storage'
  location: location
  sku: {
    name: 'Standard_LRS' // Basic tier
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

// App Service Plan (Basic tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${resourcePrefix}plan'
  location: location
  sku: {
    name: 'B1' // Basic tier
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

// App Service (Web App)
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${resourcePrefix}api'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      linuxFxVersion: 'DOTNET|9.0'
    }
  }
}

// Azure Cognitive Search (basic tier)
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: '${resourcePrefix}search'
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    hostingMode: 'default'
    partitionCount: 1
    replicaCount: 1
  }
}

// Azure AI Vision (Computer Vision, free tier)
resource visionService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: '${resourcePrefix}vision'
  location: location
  sku: {
    name: 'F0' // Free tier
  }
  kind: 'ComputerVision'
  properties: {
    apiProperties: {
      qnaRuntimeEndpoint: ''
    }
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Azure OpenAI (lowest available tier)
resource openAIService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: '${resourcePrefix}openai'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output storageAccountBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output webAppName string = webApp.name
output searchServiceName string = searchService.name
output searchServiceEndpoint string = 'https://${searchService.name}.search.windows.net'
output visionServiceName string = visionService.name
output visionServiceEndpoint string = 'https://${visionService.name}.cognitiveservices.azure.com'
output openAIServiceName string = openAIService.name
output openAIServiceEndpoint string = 'https://${openAIService.name}.openai.azure.com/'
