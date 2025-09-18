using System.Text.Json.Serialization;

namespace FurnitureFinder.API.Models;

public class FurnitureAnalysisResult
{
    public string Description { get; set; }
    
    public IEnumerable<string> Tags { get; internal set; } = [];
    
    public IEnumerable<string> OtherDescriptions { get; internal set; } = [];
    
    public string OpenAIDescription { get; internal set; }

    public string OpenAIConciseDescription { get; internal set; }

    public string CompetitorProductsString { get; internal set; }

    public CompetitorProduct[] CompetitorProducts { get; internal set; }
    
    [JsonIgnore]
    public Uri ImageUrl { get; internal set; }
    
}
