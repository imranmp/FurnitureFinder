using System.ComponentModel.DataAnnotations;

namespace FurnitureFinder.Functions.Configurations;

public class AzureConfiguration
{
    public const string ConfigurationSectionName = "AzureServices";

    [Required]
    public required SearchConfig Search { get; set; }

    [Required]
    public required OpenAIConfig OpenAI { get; set; }
}

public class SearchConfig
{
    public const string ConfigurationSectionName = "AzureServices:Search";

    [Required]
    [Url]
    public required string Endpoint { get; set; }

    [Required]
    public required string Key { get; set; }

    [Required]
    public required string IndexName { get; set; }
}

public class OpenAIConfig
{
    public const string ConfigurationSectionName = "AzureServices:OpenAI";

    [Required]
    [Url]
    public required string Endpoint { get; set; }

    [Required]
    public required string Key { get; set; }

    [Required]
    public required string DeploymentName { get; set; }

    [Required]
    public required string EmbeddingDeploymentName { get; set; }

    [Required]
    public required int EmbeddingDimensions { get; set; }
}