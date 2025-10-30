# FurnitureFinder.Functions

Azure Functions project for background processing tasks in the FurnitureFinder application.

## Functions

### 1. UpdateEmbeddingsFunction
Timer-triggered function that runs every hour to update missing vector embeddings in the Azure AI Search index.

- **Trigger Schedule**: Configurable via `UpdateEmbeddingsSchedule` setting (default: every hour)
- **Behavior**: 
  - Queries Azure AI Search for 20 documents where `vectorRetrieved = false`
  - Generates embeddings using Azure OpenAI
  - Updates documents with the generated embeddings
  - Sets `vectorRetrieved = true`

### 2. GenerateProductsFunction
Timer-triggered function that runs every hour to create realistic furniture products using AI.

- **Trigger Schedule**: Configurable via `GenerateProductsSchedule` setting (default: every hour)
- **Behavior**:
  - Uses Azure OpenAI to generate 5 new realistic furniture products
  - AI creates varied product names, descriptions, styles, colors, and features
  - Products are based on sample data patterns for consistency
  - Uploads them to the Azure AI Search index
  - Products are created with `vectorRetrieved = false` so they can be processed by UpdateEmbeddingsFunction

## Configuration

### User Secrets
This project shares the same user secrets ID as `FurnitureFinder.API`:
```
UserSecretsId: 50a86c15-bc17-4983-9cd4-bb2eea203206
```

### Required Configuration

Add the following to your user secrets or `appsettings.json`:

```json
{
"Functions": {
    "UpdateEmbeddingsSchedule": "0 0 * * * *",
    "GenerateProductsSchedule": "0 0 * * * *"
  },
  "AzureServices": {
    "Search": {
      "Endpoint": "https://<your-search-service>.search.windows.net",
      "Key": "<your-search-key>",
      "IndexName": "<your-index-name>"
    },
    "OpenAI": {
      "Endpoint": "https://<your-openai-service>.openai.azure.com/",
      "Key": "<your-openai-key>",
      "DeploymentName": "<your-chat-deployment>",
      "EmbeddingDeploymentName": "<your-embedding-deployment>",
      "EmbeddingDimensions": 1536
    }
  }
}
```

**Note**: You need two Azure OpenAI deployments:
- **DeploymentName**: A chat model (e.g., GPT-4, GPT-3.5-turbo) for generating product descriptions
- **EmbeddingDeploymentName**: An embedding model (e.g., text-embedding-ada-002) for generating vector embeddings

### Timer Schedule Format

The timer schedule uses CRON expression format: `{second} {minute} {hour} {day} {month} {day-of-week}`

Examples:
- `0 0 * * * *` - Every hour at the top of the hour
- `0 */30 * * * *` - Every 30 minutes
- `0 0 */2 * * *` - Every 2 hours
- `0 0 9 * * *` - Every day at 9:00 AM

## Local Development

### Prerequisites
- .NET 9 SDK
- Azure Functions Core Tools
- Azure Storage Emulator or Azurite

### Running Locally

1. Configure user secrets:
   ```bash
   cd src/FurnitureFinder.Functions
   dotnet user-secrets set "AzureServices:Search:Endpoint" "https://your-search.search.windows.net"
   dotnet user-secrets set "AzureServices:Search:Key" "your-key"
   dotnet user-secrets set "AzureServices:Search:IndexName" "your-index"
   dotnet user-secrets set "AzureServices:OpenAI:Endpoint" "https://your-openai.openai.azure.com/"
   dotnet user-secrets set "AzureServices:OpenAI:Key" "your-key"
   dotnet user-secrets set "AzureServices:OpenAI:DeploymentName" "your-chat-deployment"
   dotnet user-secrets set "AzureServices:OpenAI:EmbeddingDeploymentName" "your-deployment"
   dotnet user-secrets set "AzureServices:OpenAI:EmbeddingDimensions" "1536"
   ```

2. Run the Functions:
   ```bash
   func start
   ```

## Deployment

### Deploy to Azure Functions

1. Create an Azure Functions app (Linux, .NET 9)
2. Configure Application Settings with the required configuration
3. Deploy:
   ```bash
   func azure functionapp publish <function-app-name>
   ```

## Services

- **EmbeddingService**: Generates text embeddings using Azure OpenAI
- **SearchIndexService**: Interacts with Azure AI Search for CRUD operations
- **ProductGeneratorService**: Generates synthetic furniture products for testing

## Dependencies

- Azure.AI.OpenAI
- Azure.Search.Documents
- Microsoft.Azure.Functions.Worker
- Microsoft.Azure.Functions.Worker.Extensions.Timer
