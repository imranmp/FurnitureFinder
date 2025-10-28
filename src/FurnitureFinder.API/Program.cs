using FluentValidation;
using FurnitureFinder.API.Contracts.Validators;
using FurnitureFinder.API.Middleware;
using FurnitureFinder.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure strongly-typed options
AddConfigurationOptions(builder);

// Register all validators in your assembly
builder.Services.AddValidatorsFromAssemblyContaining<RecommendationRequestValidator>();

// Register application services
builder.Services.AddScoped<IAzureVisionService, AzureVisionService>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IIndexService, IndexService>();
builder.Services.AddScoped<IFurnitureFinderService, FurnitureFinderService>();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

//app.UseExceptionHandler();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.Run();

static void AddConfigurationOptions(WebApplicationBuilder builder)
{
    builder.Services.AddOptions<AzureConfiguration>()
        .Bind(builder.Configuration.GetSection(AzureConfiguration.ConfigurationSectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<VisionConfig>()
        .Bind(builder.Configuration.GetSection(VisionConfig.ConfigurationSectionName))
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

    builder.Services.AddOptions<BlobStorageConfig>()
        .Bind(builder.Configuration.GetSection(BlobStorageConfig.ConfigurationSectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
}