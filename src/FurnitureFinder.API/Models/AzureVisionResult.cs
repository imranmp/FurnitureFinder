namespace FurnitureFinder.API.Models;

public class AzureVisionResult
{
    public required string Description { get; set; }
    
    public IEnumerable<string> Tags { get; internal set; } = [];
    
    public IEnumerable<string> OtherDescriptions { get; internal set; } = [];
}
