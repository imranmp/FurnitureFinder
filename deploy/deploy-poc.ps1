# PowerShell script to deploy Bicep and configure App Service settings for FurnitureFinder POC

param(
    [string]$ResourceGroup = "<your-resource-group>",
    [string]$Location = "eastus",
    [string]$ResourcePrefix = "ffpoc"
)

# Deploy Bicep file and get outputs
$deployment = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file "main.bicep" `
    --parameters resourcePrefix=$ResourcePrefix location=$Location `
    --query "properties.outputs" -o json | ConvertFrom-Json

# Extract outputs
$storageAccountName = $deployment.storageAccountName.value
$storageAccountBlobEndpoint = $deployment.storageAccountBlobEndpoint.value
$webAppName = $deployment.webAppName.value
$searchServiceName = $deployment.searchServiceName.value
$searchServiceEndpoint = $deployment.searchServiceEndpoint.value
$visionServiceName = $deployment.visionServiceName.value
$visionServiceEndpoint = $deployment.visionServiceEndpoint.value
$openAIServiceName = $deployment.openAIServiceName.value
$openAIServiceEndpoint = $deployment.openAIServiceEndpoint.value

# Get secrets
$storageConnectionString = az storage account show-connection-string --name $storageAccountName --resource-group $ResourceGroup --query "connectionString" -o tsv
$visionKey = az cognitiveservices account keys list --name $visionServiceName --resource-group $ResourceGroup --query "key1" -o tsv
$searchKey = az search admin-key show --service-name $searchServiceName --resource-group $ResourceGroup --query "primaryKey" -o tsv
$openAIKey = az cognitiveservices account keys list --name $openAIServiceName --resource-group $ResourceGroup --query "key1" -o tsv

# Defaults for custom values
$containerName = "images"
$deploymentName = "gpt-4.1"
$indexName = "furniture-index"
$semanticConfigName = "furniture-semantic-config"

# Set App Service settings
az webapp config appsettings set --name $webAppName --resource-group $ResourceGroup --settings `
    "AzureServices__Vision__Endpoint=$visionServiceEndpoint" `
    "AzureServices__Vision__Key=$visionKey" `
    "AzureServices__Search__Endpoint=$searchServiceEndpoint" `
    "AzureServices__Search__Key=$searchKey" `
    "AzureServices__Search__IndexName=$indexName" `
    "AzureServices__Search__SemanticConfigurationName=$semanticConfigName" `
    "AzureServices__OpenAI__Endpoint=$openAIServiceEndpoint" `
    "AzureServices__OpenAI__Key=$openAIKey" `
    "AzureServices__OpenAI__DeploymentName=$deploymentName" `
    "AzureServices__BlobStorage__ConnectionString=$storageConnectionString" `
    "AzureServices__BlobStorage__ContainerName=$containerName"

Write-Host "App Service settings updated for $webAppName."
