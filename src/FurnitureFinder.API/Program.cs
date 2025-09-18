using FurnitureFinder.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

// Configure Azure services
builder.Services.AddSingleton<IValidateOptions<AzureConfiguration>, AzureConfigurationValidator>();

builder.Services.AddOptions<AzureConfiguration>()
    .Bind(builder.Configuration.GetSection(AzureConfiguration.ConfigurationSectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register services
builder.Services.AddScoped<IComputerVisionService, ComputerVisionService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

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
