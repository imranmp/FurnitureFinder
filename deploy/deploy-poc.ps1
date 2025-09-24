# PowerShell script to deploy Bicep and configure App Service settings for FurnitureFinder POC

param(
    [string]$ResourceGroup = "azure-ai-services",
    [string]$Location = "eastus",
    [string]$ResourcePrefix = "ff-poc-"
)

# Set error handling
$ErrorActionPreference = "Stop"

# Get the script directory to ensure correct paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$bicepFile = Join-Path $scriptDir "main.bicep"

Write-Host "Creating resource group if it doesn't exist..."
az group create --name $ResourceGroup --location $Location

Write-Host "Deploying Bicep template..."
# Deploy Bicep file and get outputs
$deploymentOutput = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file $bicepFile `
    --parameters resourcePrefix=$ResourcePrefix location=$Location `
    --query "properties.outputs" -o json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Bicep deployment failed!"
    exit 1
}

$deployment = $deploymentOutput | ConvertFrom-Json

if (-not $deployment) {
    Write-Error "Failed to parse deployment outputs!"
    exit 1
}

# Extract outputs
$storageAccountName = $deployment.storageAccountName.value
# $storageAccountBlobEndpoint = $deployment.storageAccountBlobEndpoint.value
$webAppName = $deployment.webAppName.value
$searchServiceName = $deployment.searchServiceName.value
$searchServiceEndpoint = $deployment.searchServiceEndpoint.value
$visionServiceName = $deployment.visionServiceName.value
$visionServiceEndpoint = $deployment.visionServiceEndpoint.value
$openAIServiceName = $deployment.openAIServiceName.value
$openAIServiceEndpoint = $deployment.openAIServiceEndpoint.value

Write-Host "Deployment successful! Extracted resource names:"
Write-Host "Storage Account: $storageAccountName"
Write-Host "Web App: $webAppName"
Write-Host "Search Service: $searchServiceName"
Write-Host "Vision Service: $visionServiceName"
Write-Host "OpenAI Service: $openAIServiceName"

Write-Host "Retrieving service keys and connection strings..."
# Get secrets
$storageConnectionString = az storage account show-connection-string --name $storageAccountName --resource-group $ResourceGroup --query "connectionString" -o tsv
$visionKey = az cognitiveservices account keys list --name $visionServiceName --resource-group $ResourceGroup --query "key1" -o tsv
$searchKey = az search admin-key show --service-name $searchServiceName --resource-group $ResourceGroup --query "primaryKey" -o tsv
$openAIKey = az cognitiveservices account keys list --name $openAIServiceName --resource-group $ResourceGroup --query "key1" -o tsv

# Verify all keys were retrieved
if (-not $storageConnectionString -or -not $visionKey -or -not $searchKey -or -not $openAIKey) {
    Write-Error "Failed to retrieve one or more service keys!"
    exit 1
}

# Defaults for custom values
$containerName = "images"
$deploymentName = "gpt-4.1"
$indexName = "furniture-index"
$semanticConfigName = "furniture-semantic-config"

Write-Host "Configuring App Service settings..."
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

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ App Service settings updated successfully for $webAppName"
    Write-Host ""
    Write-Host "Deployment Summary:"
    Write-Host "==================="
    Write-Host "Resource Group: $ResourceGroup"
    Write-Host "Location: $Location"
    Write-Host "Resource Prefix: $ResourcePrefix"
    Write-Host "Web App URL: https://$webAppName.azurewebsites.net"
} else {
    Write-Error "Failed to update App Service settings!"
    exit 1
}
