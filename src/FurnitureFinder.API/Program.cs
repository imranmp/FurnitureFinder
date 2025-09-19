using FurnitureFinder.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

// Configure Azure services
//builder.Services.AddSingleton<IValidateOptions<AzureConfiguration>, AzureConfigurationValidator>();
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

// Register services
builder.Services.AddScoped<IAzureVisionService, AzureVisionService>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

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
