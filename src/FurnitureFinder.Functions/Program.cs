using FurnitureFinder.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add appsettings.json to configuration
//builder.Configuration.Sources.Clear();

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(Environment.GetCommandLineArgs())
    .AddUserSecrets<Program>(optional: true);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure strongly-typed options
builder.Services.AddOptions<AzureConfiguration>()
    .Bind(builder.Configuration.GetSection(AzureConfiguration.ConfigurationSectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SearchConfig>()
    .Bind(builder.Configuration.GetSection(SearchConfig.ConfigurationSectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OpenAIConfig>()
    .Bind(builder.Configuration.GetSection(OpenAIConfig.ConfigurationSectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register application services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ISearchIndexService, SearchIndexService>();
builder.Services.AddScoped<IProductGeneratorService, ProductGeneratorService>();


builder.Build().Run();
