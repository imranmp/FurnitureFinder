using System.ComponentModel.DataAnnotations;

namespace FurnitureFinder.API.Configurations;

public class AzureConfiguration()
{
    public const string ConfigurationSectionName = "AzureServices";

    [Required]
    public required ComputerVisionConfig ComputerVision { get; set; }

    [Required]
    public required SearchConfig Search { get; set; }

    [Required]
    public required OpenAIConfig OpenAI { get; set; }

    [Required]
    public required BlobStorage BlobStorage { get; set; }
}

public class ComputerVisionConfig()
{
    public required string Endpoint { get; set; }

    public required string Key { get; set; }
}

public class SearchConfig
{
    public required string Endpoint { get; set; }

    public required string Key { get; set; }

    public required string IndexName { get; set; }

    public required string SemanticConfigurationName { get; set; }
}

public class OpenAIConfig()
{
    public required string Endpoint { get; set; }

    public required string Key { get; set; }

    public required string DeploymentName { get; set; }
}

public class BlobStorage()
{
    public required string ConnectionString { get; set; }

    public required string ContainerName { get; set; }
}