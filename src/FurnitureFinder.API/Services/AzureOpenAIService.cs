using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace FurnitureFinder.API.Services;

public class AzureOpenAIService(IOptions<OpenAIConfig> openAIConfig)
    : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _client = new(
            new Uri(openAIConfig.Value.Endpoint),
            new AzureKeyCredential(openAIConfig.Value.Key));

    private readonly string _deploymentName = openAIConfig.Value.DeploymentName;

    public async Task<string> GetImageDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Based on the Image URL of the furniture and analysis from Azure Vision AI:
            - Caption: {analysis.Description}
            - DenseCaptions: {string.Join(", ", analysis.OtherDescriptions)}
            - Tag: {string.Join(", ", analysis.Tags)}
            - Image URL: {imageUrl}
    
            Create a detailed e-commerce product description following this structure:

            1. Opening statement (1-2 sentences):
               - Main furniture type
               - Primary distinctive features
               - Target room/setting

            2. Design & Style (2-3 sentences):
               - Design style/period
               - Color scheme
               - Visual elements
               - Aesthetic appeal

            3. Construction & Materials (1-2 sentences):
               - Primary materials
               - Build quality indicators
               - Notable finishes

            4. Functional Features (1-2 sentences):
               - Dimensions (if visible)
               - Practical features
               - Usage scenarios

            Keep the tone professional yet engaging. Focus on features that drive purchase decisions.
            Avoid subjective claims unless clearly supported by visual evidence.
            """;

        var chatClient = _client.GetChatClient(_deploymentName);

        var chatMessages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage("""
                You are a professional furniture copywriter specializing in e-commerce product descriptions.
                Create compelling, SEO-friendly descriptions that:
                - Emphasize unique selling points
                - Use industry-standard terminology
                - Balance detail with readability
                - Maintain factual accuracy
                - Follow consistent paragraph structure
                """),

            ChatMessage.CreateUserMessage(prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 200,
            Temperature = 0.7f
        };

        ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);

        // Defensive check for response content
        if (response?.Value?.Content is { Count: > 0 })
        {
            return response.Value.Content[0].Text;
        }

        return "No description generated.";
    }

    public async Task<string> GetConciseDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Based on the Image URL of the furniture and analysis from Azure Vision AI:
            - Caption: {analysis.Description}
            - DenseCaptions: {string.Join(", ", analysis.OtherDescriptions)}
            - Tag: {string.Join(", ", analysis.Tags)}
            - Image URL: {imageUrl}

            Provide TWO outputs in the exact format shown:

            1. A single-sentence keyword-rich description of the furniture piece (max 30 words) emphasizing:
               - Furniture category
               - Furniture subcategory
               - Key distinguishing features
               - Main materials
               - Dominant colors
               - Design style

            2. Structured attributes (exactly as shown):
                Furniture category: [single type only]
                Furniture subcategory: [single type only]
                Style: [up to 2 most relevant styles]
                Color: [up to 3 dominant colors]
                Material: [up to 3 primary materials]

            Furniture category examples: Seating, Tables, Storage, Bedroom, Dining Room, Lighting, Decor & Accessories, Textiles
            Furniture subcategory examples: Sofas, Sectionals, Loveseats, Chairs, Recliners, Benches, Ottomans, Bar Stools, Counter Stools, Dining Tables, Coffee Tables, End Tables, Side Tables, Console Tables, Desks, Nightstands, Bar Tables, Accent Tables, Dressers, Chests, Bookcases, Shelving Units, TV Stands, Media Centers, Cabinets, Wardrobes, Armoires, Storage Benches, Storage Ottomans, Beds, Headboards, Footboards, Bed Frames, Mattresses, Box Springs, Bedroom Sets, Vanities, Mirrors (bedroom), Dining Sets, China Cabinets, Hutches, Buffets, Sideboards, Wine Racks, Serving Carts, Table Lamps, Floor Lamps, Desk Lamps, Ceiling Fixtures, Chandeliers, Pendant Lights, Wall Sconces, Track Lighting, Accent Lighting, Smart Lighting, Wall Art, Paintings, Prints, Photography, Posters, Canvas Art, Mirrors, Decorative Mirrors, Wall Mirrors, Vases, Planters, Decorative Bowls, Sculptures, Figurines, Picture Frames, Wall Clocks, Artificial Plants, Artificial Flowers, Area Rugs, Runner Rugs, Throw Pillows, Decorative Pillows, Throw Blankets, Curtains, Drapes, Window Treatments, Blinds, Shades, Valances, Bedding Sets, Comforters, Quilts, Sheets
            Style examples: abstract, bohemian, casual, classic, coastal, comfortable, contemporary, cozy, country, craftsman, eclectic, family-friendly, farmhouse, fun, functional, glam, global, industrial, luxurious, mid-century modern, minimalist, modern, modular, natural, retro, romantic, rustic, scandinavian, traditional, transitional, urban, vintage
            Color examples: Black, White, Gray, Brown, Blue, Green, Red, Yellow, Orange, Purple, Pink, Gold, Silver, Beige, Tan, Cream, etc.
            Material examples: Wood, Metal, Fabric, Leather, Glass, Ceramic, Plastic, Stone, Composite, Hardware, Foam, Natural Fibers, Synthetic Fibers, Finishes, etc.
            
            Do not include unnecessary details.

            Example Output:
            Sophisticated contemporary area rug with navy background and yellow geometric pattern.

            Furniture category: Textiles
            Furniture subcategory: Area Rugs
            Style: Contemporary, abstract
            Color: navy blue, off-white, bright yellow
            Material: fabric, synthetic fibers, jute
            """;

        var chatClient = _client.GetChatClient(_deploymentName);

        var chatMessages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage("""
                You are an expert in furniture classification and description.
                Generate precise, structured output following the exact format requested.
                Focus on objective, observable characteristics rather than subjective qualities.
                Maintain consistency between the narrative description and structured attributes.
                Always return data that strictly follows the requested format.
                """),

            ChatMessage.CreateUserMessage(prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 80,
            Temperature = 0.7f
        };

        ClientResult<ChatCompletion> response = await chatClient.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);

        if (response?.Value?.Content is { Count: > 0 })
        {
            return response.Value.Content[0].Text;
        }

        return "No concise description generated.";
    }
}
