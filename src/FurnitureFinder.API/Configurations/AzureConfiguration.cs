using System.ComponentModel.DataAnnotations;

namespace FurnitureFinder.API.Configurations;

public class AzureConfiguration()
{
    public const string ConfigurationSectionName = "AzureServices";

    [Required]
    public required VisionConfig Vision { get; set; }

    [Required]
    public required SearchConfig Search { get; set; }

    [Required]
    public required OpenAIConfig OpenAI { get; set; }

    [Required]
    public required BlobStorageConfig BlobStorage { get; set; }
}

public class VisionConfig()
{
    public const string ConfigurationSectionName = "AzureServices:Vision";

    [Required]
    [Url]
    public required string Endpoint { get; set; }

    [Required]
    public required string Key { get; set; }
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

    [Required]
    public required string SemanticConfigurationName { get; set; }
}

public class OpenAIConfig()
{
    public const string ConfigurationSectionName = "AzureServices:OpenAI";

    [Required]
    [Url]
    public required string Endpoint { get; set; }

    [Required]
    public required string Key { get; set; }

    [Required]
    public required string DeploymentName { get; set; }
}

public class BlobStorageConfig()
{
    public const string ConfigurationSectionName = "AzureServices:BlobStorage";

    [Required]
    public required string ConnectionString { get; set; }

    [Required]
    public required string ContainerName { get; set; }
}