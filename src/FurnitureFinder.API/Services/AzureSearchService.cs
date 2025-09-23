using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Text.RegularExpressions;

namespace FurnitureFinder.API.Services;

public class AzureSearchService(IOptions<SearchConfig> searchConfig, ILogger<AzureSearchService> logger)
    : IAzureSearchService
{
    private readonly SearchClient _searchClient = new(
            new Uri(searchConfig.Value.Endpoint),
            searchConfig.Value.IndexName,
            new Azure.AzureKeyCredential(searchConfig.Value.Key));

    private readonly string _semanticConfigurationName = searchConfig.Value.SemanticConfigurationName;

    public async Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        try
        {
            if (products == null || !products.Any())
            {
                products = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText("sample-data/sample_furniture_data.json")) ?? [];
            }

            Response<IndexDocumentsResult> result = await _searchClient.MergeOrUploadDocumentsAsync<Product>(products, cancellationToken: cancellationToken);

            result.Value.Results.ToList().ForEach(r =>
            {
                if (!r.Succeeded)
                {
                    logger.LogWarning("Failed to index document with key: {Key}, Error: {ErrorMessage}", r.Key, r.ErrorMessage);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error populating search index.");
        }
    }

    public async Task<(string, List<ProductSearchResult>)> FindComplementaryFurnitureAsync(AzureVisionResult azureVisionResult, string openAIConciseDescription,
        RecommendationRequest request, CancellationToken cancellationToken = default)
    {
        // Extract category from search text (e.g., "rugs" from "show me rugs that go with this chair")
        //string? targetCategory = ExtractTargetCategory(request.SearchText);

        var tags = ExtractTags(openAIConciseDescription);

        var options = new SearchOptions
        {
            Size = 20,
            QueryType = SearchQueryType.Semantic, // Enable semantic search
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _semanticConfigurationName,
            },
            Filter = BuildFilterExpression(tags, targetCategory: null),
        };

        // Use only the search intent for semantic search
        //string query = request.SearchText ?? string.Empty;
        string query = GenerateSearchText(tags, azureVisionResult.Description);

        // Execute search
        var results = await _searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);

        var recommendations = new List<ProductSearchResult>();

        // Collect all results first
        var allResults = new List<(SearchResult<SearchDocument> Result, ProductSearchResult Product)>();

        await foreach (var result in results.Value.GetResultsAsync())
        {
            // For semantic search, use RerankerScore with threshold around 1.0-1.5
            if (result.SemanticSearch?.RerankerScore == null)
            {
                continue;
            }

            SearchDocument doc = result.Document;

            var product = new ProductSearchResult(doc.GetString("id"),
                doc.GetString("TopCategory"),
                doc.GetString("Category"),
                doc.GetString("AdvertisingCopy"),
                doc.GetString("DescriptionTitle"),
                doc.GetString("Decor"),
                doc.GetString("Theme"),
                doc.GetString("PrimaryColor"),
                doc.GetString("DetailCategory"),
                doc.GetString("ReferenceColor"),
                doc.GetString("ProductFactoryFinish"),
                doc.GetString("Material"),
                doc.GetString("SKU"),
                doc.GetString("PrimaryFinish"));

            allResults.Add((result, product));
        }

        // Sort by RerankerScore in descending order and take the products
        recommendations = allResults
            .OrderByDescending(x => x.Result.SemanticSearch?.RerankerScore) // Then by semantic score
                                                                            //.OrderByDescending(x => x.Result.Score) // Then by semantic score
                                                                            //.ThenByDescending(x => x.Result.SemanticSearch?.RerankerScore) // Then by semantic score
            .Select(x => x.Product)
            .ToList();

        //return (query, recommendations);
        return ($"Filter: {options.Filter}: Query: {query}", recommendations);
    }

    private static string GenerateSearchText(FurnitureTags tags, string aiDescription)
    {
        // Generate search text from OpenAIConciseDescription
        var searchParts = new List<string>();

        //// Add furniture type if available
        //if (!string.IsNullOrWhiteSpace(tags.FurnitureType))
        //{
        //    searchParts.Add($"Find items in {tags.FurnitureType} category");
        //}

        if (!string.IsNullOrWhiteSpace(aiDescription))
        {
            searchParts.Add($"Find items that match {aiDescription}");
        }

        // Add style preferences
        if (tags.Styles?.Length > 0)
        {
            searchParts.Add($"in {string.Join(" or ", tags.Styles)} theme or decor");
        }

        // Add color preferences
        if (tags.Colors?.Length > 0)
        {
            searchParts.Add($"with {string.Join(" or ", tags.Colors)} colors");
        }

        //// Add material preferences
        //if (tags.Materials?.Length > 0)
        //{
        //    searchParts.Add($"made of {string.Join(" or ", tags.Materials)}");
        //}

        return string.Join(" ", searchParts);
    }

    private static string? BuildFilterExpression(FurnitureTags tags, string? targetCategory)
    {
        var filters = new List<string>();
        filters.Add($"search.in(TopCategory, 'ADULT', '|')");

        ////Filter by target category if specified
        //if (!string.IsNullOrWhiteSpace(tags.FurnitureType))
        //{
        //    //filters.Add($"search.in(Category, '{tags.FurnitureType}', '|') and (search.in(TopCategory, 'ADULT', '|') or search.in(TopCategory, 'KID', '|'))");
        //    filters.Add($"search.in(Category, '{tags.FurnitureType}', '|') and search.in(TopCategory, 'ADULT', '|')");
        //}
        //else
        //{
        //    filters.Add($"search.in(TopCategory, 'ADULT', '|') or search.in(TopCategory, 'KID', '|')");
        //}

        //// Add color filter if available
        //if (tags.Colors.Length > 0)
        //{
        //    var escapedColors = string.Join("|", tags.Colors.Select(c => c.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(PrimaryColor, '{escapedColors}', '|') or search.in(ReferenceColor, '{escapedColors}', '|')");
        //}

        //// Add material filter if available
        //if (tags.Materials?.Length > 0)
        //{
        //    var escapedMaterials = string.Join("|", tags.Materials.Select(m => m.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(Material, '{escapedMaterials}', '|')");
        //}

        //// Add style/theme filter if available
        //if (tags.Styles?.Length > 0)
        //{
        //    var escapedStyles = string.Join("|", tags.Styles.Select(s => s.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(Theme, '{escapedStyles}', '|') or search.in(Decor, '{escapedStyles}', '|')");
        //}

        return filters.Count > 0 ? string.Join(" and ", filters) : null;
    }

    private static FurnitureTags ExtractTags(string openAIConciseDescription)
    {
        FurnitureTags furnitureTags = new();

        if (string.IsNullOrWhiteSpace(openAIConciseDescription))
        {
            return furnitureTags;
        }

        // Regex pattern to capture key-value pairs
        string pattern = @"(?<key>Furniture Type|Style|Color|Material)\s*:\s*(?<value>.+)";
        var matches = Regex.Matches(openAIConciseDescription, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            string key = match.Groups["key"].Value.Trim();
            string value = match.Groups["value"].Value.Trim();

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "furniture type":
                    furnitureTags.FurnitureType = value;
                    break;
                case "style":
                    furnitureTags.Styles = value.Split(",");
                    break;
                case "color":
                    furnitureTags.Colors = value.Split(",");
                    break;
                case "material":
                    furnitureTags.Materials = value.Split(",");
                    break;
            }
        }

        return furnitureTags;
    }

    public class FurnitureTags
    {
        public string FurnitureType { get; set; } = string.Empty;

        public string[] Colors { get; internal set; }

        public string[] Materials { get; internal set; }

        public string[] Styles { get; internal set; }
    }
}
