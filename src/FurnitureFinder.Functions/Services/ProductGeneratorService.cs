using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace FurnitureFinder.Functions.Services;

public class ProductGeneratorService(IOptions<OpenAIConfig> openAIConfig,
                                     ILogger<ProductGeneratorService> logger) : IProductGeneratorService
{

    private readonly AzureOpenAIClient _client = new(new Uri(openAIConfig.Value.Endpoint),
                                                     new AzureKeyCredential(openAIConfig.Value.Key));

    private readonly string _deploymentName = openAIConfig.Value.DeploymentName;

    private const string SampleProducts = """
            {
                "id": "F9E8C1FA-AA0C-475B-8844-121364F14878",
                "sku": "SOF-MDN-CHR-001",
                "name": "Modern Charcoal Sectional Sofa",
                "description": "Spacious L-shaped sectional sofa with clean lines and plush cushioning, perfect for contemporary living spaces.",
                "category": "Seating",
                "subcategory": "Sectionals",
                "price": 1299.99,
                "style": ["modern", "contemporary"],
                "colors": {"primary": "charcoal gray", "secondary": "black", "all_colors": ["charcoal gray", "black"]},
                "materials": ["fabric", "hardwood frame"],
                "room_types": ["living room"],
                "features": ["reversible chaise", "removable cushions"],
                "tags": ["spacious", "family-friendly", "contemporary"]
            }
        """;

    public async Task<List<Product>> GenerateProductsAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            List<Product> products = [];

            var prompt = $"""
                        You are a furniture product catalog generator. Generate {count} unique, realistic furniture products in JSON format.

                        Follow the exact same structure as the examples provided below. Be creative with:
                        - Product names (descriptive and appealing)
                        - Detailed descriptions (highlight key features and benefits)
                        - Varied categories, subcategories, styles, colors, and materials
                        - Realistic pricing ($100-$3000 range)
                        - Appropriate features and tags for each product

                        Categories: Seating, Tables, Storage, Bedroom, Textiles, Lighting, Decor & Accessories
                        Styles: modern, contemporary, traditional, rustic, industrial, mid-century modern, scandinavian, bohemian, coastal, farmhouse, minimalist, vintage, glam

                        Here are example products to follow:

                        {SampleProducts}

                        Generate EXACTLY {count} NEW furniture product (not the examples above). Return ONLY a valid JSON with no additional text.
                        Product must have a unique id (use Guid), unique SKU, and be different from the examples.
                        """;

            var chatClient = _client.GetChatClient(_deploymentName);

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage($"""
                    You are an expert furniture product catalog writer. Generate realistic, appealing furniture product descriptions.

                    Always return a valid JSON array of {count} products following the exact structure provided in examples. 
                    Be creative but realistic with product details, descriptions, and specifications.
                    Ensure all generated products are unique and have diverse attributes.
                    """),

                ChatMessage.CreateUserMessage(prompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 2000,
                Temperature = 0.9f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);

            if (response?.Value?.Content is { Count: > 0 })
            {
                var jsonContent = response.Value.Content[0].Text;
                logger.LogInformation("Received AI-generated products JSON");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                try
                {
                    products = JsonSerializer.Deserialize<List<Product>>(jsonContent, jsonOptions) ?? [];
                }
                catch
                {
                    var wrapper = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                    if (wrapper.TryGetProperty("product", out var productsArray))
                    {
                        products = JsonSerializer.Deserialize<List<Product>>(productsArray.GetRawText(), jsonOptions) ?? [];
                    }
                }

                foreach (var product in products)
                {
                    product.VectorRetrieved = false;
                }

                logger.LogInformation("Successfully generated {Count} furniture products using AI. {JSON}", products.Count, jsonContent);
                return products;
            }

            logger.LogWarning("No products generated from AI response, returning empty list");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating products using AI");
            throw;
        }
    }
}