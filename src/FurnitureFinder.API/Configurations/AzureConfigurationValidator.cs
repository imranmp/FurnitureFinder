namespace FurnitureFinder.API.Configurations;

public class AzureConfigurationValidator : IValidateOptions<AzureConfiguration>
{
    public ValidateOptionsResult Validate(string? name, AzureConfiguration settings)
    {
        StringBuilder errors = new();

        if (settings is null)
        {
            errors.AppendLine("Azure configuration is required.");
            return ValidateOptionsResult.Fail(errors.ToString());
        }

        if (settings.Vision is null)
        {
            errors.AppendLine("Computer Vision configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.Vision.Endpoint))
            {
                errors.AppendLine("Computer Vision endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.Vision.Key))
            {
                errors.AppendLine("Computer Vision key is required.");
            }
        }


        if (settings.Search is null)
        {
            errors.AppendLine("Search configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.Search.Endpoint))
            {
                errors.AppendLine("Search endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.Search.Key))
            {
                errors.AppendLine("Search key is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.Search.IndexName))
            {
                errors.AppendLine("Search index name is required.");
            }
        }

        if (settings.OpenAI is null)
        {
            errors.AppendLine("OpenAI configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.OpenAI.Endpoint))
            {
                errors.AppendLine("OpenAI endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.OpenAI.Key))
            {
                errors.AppendLine("OpenAI key is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.OpenAI.DeploymentName))
            {
                errors.AppendLine("OpenAI deployment name is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.OpenAI.EmbeddingDeploymentName))
            {
                errors.AppendLine("OpenAI embedding deployment name is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.OpenAI.EmbeddingModelName))
            {
                errors.AppendLine("OpenAI embedding model name is required.");
            }

            if (settings.OpenAI.EmbeddingDimensions == 0)
            {
                errors.AppendLine("OpenAI embedding dimensions value is required.");
            }
        }

        if (settings.BlobStorage is null)
        {
            errors.AppendLine("Blob Storage configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.BlobStorage.ConnectionString))
            {
                errors.AppendLine("Blob Storage connection string is required.");
            }

            if (string.IsNullOrWhiteSpace(settings.BlobStorage.ContainerName))
            {
                errors.AppendLine("Blob Storage container name is required.");
            }
        }

        return errors.Length > 0
            ? ValidateOptionsResult.Fail(errors.ToString())
            : ValidateOptionsResult.Success;
    }
}
